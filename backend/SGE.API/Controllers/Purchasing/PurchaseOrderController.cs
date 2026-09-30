using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;
using SGE.Application.DTOs.Payment;
using SGE.Application.DTOs.PurchaseOrder;
using SGE.Application.DTOs.Receipt;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;
using SGE.API.Services.FileStorage;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseOrderController : ControllerBase
{
    private readonly IPurchaseOrderService _service;
    private readonly IReceiptService _receiptService;
    private readonly IPaymentService _paymentService;
    private readonly IPurchaseOrderPdfService _pdfService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;

    public PurchaseOrderController(
        IPurchaseOrderService service,
        IReceiptService receiptService,
        IPaymentService paymentService,
        IPurchaseOrderPdfService pdfService,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage)
    {
        _service = service;
        _receiptService = receiptService;
        _paymentService = paymentService;
        _pdfService = pdfService;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
    }

    // GET: api/PurchaseOrder
    [Authorize(Roles = AppRoles.PurchaseOrderReaders)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetAll()
    {
        var purchaseOrders = await _service.GetAllAsync();

        if (!CanViewFinancialData())
            return Ok(purchaseOrders.Select(RedactFinancialData));

        return Ok(purchaseOrders);
    }

    // GET: api/PurchaseOrder/{id}
    [Authorize(Roles = AppRoles.PurchaseOrderReaders)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(Guid id)
    {
        var purchaseOrder = await _service.GetByIdAsync(id);

        if (purchaseOrder == null)
            return NotFound();

        if (!CanViewFinancialData())
            return Ok(RedactFinancialData(purchaseOrder));

        return Ok(purchaseOrder);
    }

    [Authorize(Roles = AppRoles.PurchaseOrderReaders)]
    [HttpGet("by-number/{number}")]
    public async Task<ActionResult<PurchaseOrderReceiptViewDto>> GetByNumber(
        string number)
    {
        var purchaseOrder = await _service.GetByNumberAsync(number);

        if (purchaseOrder == null)
            return NotFound();

        return Ok(purchaseOrder);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<PurchaseOrderDto>> Approve(Guid id)
    {
        try
        {
            var purchaseOrder = await _service.ApproveAsync(id, _currentUserService.UserId);

            if (purchaseOrder == null)
                return NotFound();

            return Ok(purchaseOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpPost("{id:guid}/approve-payment")]
    public async Task<ActionResult<PurchaseOrderDto>> ApprovePayment(Guid id)
    {
        try
        {
            var purchaseOrder = await _service.ApprovePaymentAsync(
                id,
                _currentUserService.UserId);

            if (purchaseOrder == null)
                return NotFound();

            return Ok(purchaseOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.BuyerOrAdmin)]
    [HttpPost("{id:guid}/mark-as-sent")]
    public async Task<ActionResult<PurchaseOrderDto>> MarkAsSent(Guid id)
    {
        try
        {
            var purchaseOrder = await _service.MarkAsSentAsync(id, _currentUserService.UserId);

            if (purchaseOrder == null)
                return NotFound();

            return Ok(purchaseOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.PurchaseOrderReaders)]
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        try
        {
            var purchaseOrder = await _service.GetByIdAsync(id);

            if (purchaseOrder == null)
                return NotFound();

            var pdf = _pdfService.Generate(purchaseOrder);
            var fileName = SanitizeFileName($"{purchaseOrder.Number}.pdf");

            return File(pdf, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.WarehouseOrAdmin)]
    [RequestSizeLimit(15 * 1024 * 1024)]
    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<ReceiptDto>> Receive(
        Guid id,
        [FromForm] string payload,
        [FromForm] string? invoiceNumber,
        [FromForm] IFormFile? invoiceFile)
    {
        string? storedRelativePath = null;
        try
        {
            if (invoiceFile == null)
                throw new ArgumentException("Anexe a Nota Fiscal para registrar o recebimento.");

            ValidateInvoiceFile(invoiceFile);

            var dto = JsonSerializer.Deserialize<ReceivePurchaseOrderDto>(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            if (dto == null)
                throw new ArgumentException("Os dados do recebimento sao invalidos.");

            dto.ReceivedByUserId = _currentUserService.UserId;

            var extension = Path.GetExtension(invoiceFile.FileName).ToLowerInvariant();
            await using (var stream = invoiceFile.OpenReadStream())
                storedRelativePath = await _fileStorage.UploadAsync(stream, $"receipts/purchase-orders/{id:N}/invoices", extension, invoiceFile.ContentType, HttpContext.RequestAborted, invoiceFile.Length);

            var receipt = await _receiptService.ReceiveAsync(id, dto);

            if (receipt == null)
            {
                if (storedRelativePath != null) await _fileStorage.DeleteAsync(storedRelativePath, HttpContext.RequestAborted);
                return NotFound();
            }

            var updated = await _receiptService.AttachInvoiceAsync(
                receipt.Id,
                invoiceNumber,
                Path.GetFileName(invoiceFile.FileName),
                storedRelativePath);

            return Ok(updated ?? receipt);
        }
        catch (ArgumentException ex)
        {
            if (storedRelativePath != null) await _fileStorage.DeleteAsync(storedRelativePath, HttpContext.RequestAborted);
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            if (storedRelativePath != null) await _fileStorage.DeleteAsync(storedRelativePath, HttpContext.RequestAborted);
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(storedRelativePath))
                await _fileStorage.DeleteAsync(storedRelativePath, HttpContext.RequestAborted);
            throw;
        }
    }

    private static readonly HashSet<string> AllowedInvoiceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private static readonly HashSet<string> AllowedInvoiceContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png"
    };

    private static void ValidateInvoiceFile(IFormFile file)
    {
        if (file.Length <= 0)
            throw new ArgumentException("A Nota Fiscal anexada esta vazia.");
        if (file.Length > 10 * 1024 * 1024)
            throw new ArgumentException("A Nota Fiscal deve ter no maximo 10 MB.");
        if (!AllowedInvoiceExtensions.Contains(Path.GetExtension(file.FileName)))
            throw new ArgumentException("A Nota Fiscal deve ser PDF, JPG, JPEG ou PNG.");
        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !AllowedInvoiceContentTypes.Contains(file.ContentType))
            throw new ArgumentException("O tipo da Nota Fiscal e invalido.");
        if (Path.GetFileName(file.FileName) != file.FileName)
            throw new ArgumentException("Nome de arquivo invalido.");
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpPost("{id:guid}/pay")]
    public async Task<ActionResult<PaymentDto>> Pay(
        Guid id,
        [FromBody] PayPurchaseOrderDto dto)
    {
        try
        {
            dto.PaidByUserId = _currentUserService.UserId;
            var payment = await _paymentService.PayAsync(id, dto);

            if (payment == null)
                return NotFound();

            return Ok(payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = new string(Path.GetInvalidFileNameChars());
        var invalidPattern = $"[{Regex.Escape(invalidCharacters)}]";

        return Regex.Replace(fileName, invalidPattern, "-");
    }

    private bool CanViewFinancialData()
    {
        return User.IsInRole(AppRoles.Finance) || User.IsInRole(AppRoles.Admin);
    }

    private static object RedactFinancialData(PurchaseOrderDto order)
    {
        return new
        {
            order.Id,
            order.QuotationId,
            order.SupplierId,
            order.SupplierName,
            order.SupplierDocument,
            order.PurchaseRequestId,
            order.PurchaseRequestNumber,
            order.WorkId,
            order.WorkName,
            order.QuotationNumber,
            order.Number,
            order.IssueDate,
            order.ExpectedDeliveryDate,
            order.Status,
            Items = order.Items.Select(item => new
            {
                item.Id,
                item.PurchaseOrderId,
                item.ItemId,
                item.ItemDescription,
                item.QuantityOrdered,
                item.QuantityReceived,
                item.QuantityPending,
                item.Unit,
                item.Observation
            })
        };
    }
}
