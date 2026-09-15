using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using SGE.Application.DTOs.Payment;
using SGE.Application.DTOs.PurchaseOrder;
using SGE.Application.DTOs.Receipt;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;

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

    public PurchaseOrderController(
        IPurchaseOrderService service,
        IReceiptService receiptService,
        IPaymentService paymentService,
        IPurchaseOrderPdfService pdfService,
        ICurrentUserService currentUserService)
    {
        _service = service;
        _receiptService = receiptService;
        _paymentService = paymentService;
        _pdfService = pdfService;
        _currentUserService = currentUserService;
    }

    // GET: api/PurchaseOrder
    [Authorize(Roles = AppRoles.PurchaseOrderReaders)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetAll()
    {
        var purchaseOrders = await _service.GetAllAsync();

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

    [Authorize(Roles = AppRoles.ApproverOrAdmin)]
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
    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<ReceiptDto>> Receive(
        Guid id,
        [FromBody] ReceivePurchaseOrderDto dto)
    {
        try
        {
            dto.ReceivedByUserId = _currentUserService.UserId;
            var receipt = await _receiptService.ReceiveAsync(id, dto);

            if (receipt == null)
                return NotFound();

            return Ok(receipt);
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
}
