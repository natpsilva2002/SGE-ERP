using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Receipt;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;
using SGE.API.Services.FileStorage;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.ReceiptReaders)]
public class ReceiptController : ControllerBase
{
    private const long MaxInvoiceSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedInvoiceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    private static readonly HashSet<string> AllowedInvoiceContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    private readonly IReceiptService _service;
    private readonly IFileStorage _fileStorage;

    public ReceiptController(
        IReceiptService service,
        IFileStorage fileStorage)
    {
        _service = service;
        _fileStorage = fileStorage;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReceiptDto>>> GetAll()
    {
        var receipts = await _service.GetAllAsync();

        return Ok(receipts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReceiptDto>> GetById(Guid id)
    {
        var receipt = await _service.GetByIdAsync(id);

        if (receipt == null)
            return NotFound();

        return Ok(receipt);
    }

    [HttpGet("by-purchase-order/{purchaseOrderId:guid}")]
    public async Task<ActionResult<IEnumerable<ReceiptDto>>> GetByPurchaseOrder(
        Guid purchaseOrderId)
    {
        var receipts = await _service.GetByPurchaseOrderIdAsync(purchaseOrderId);

        return Ok(receipts);
    }

    [Authorize(Roles = AppRoles.WarehouseOrAdmin)]
    [HttpPost("{id:guid}/invoice")]
    public async Task<ActionResult<ReceiptDto>> UploadInvoice(
        Guid id,
        [FromForm] string? invoiceNumber,
        [FromForm] IFormFile? file)
    {
        string? storedKey = null;
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceNumber) && file == null)
                throw new ArgumentException(
                    "Informe o numero da nota fiscal ou anexe o arquivo da nota fiscal.");

            if (file != null)
            {
                ValidateInvoiceFile(file);

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                await using var stream = file.OpenReadStream();
                storedKey = await _fileStorage.UploadAsync(stream, $"receipts/purchase-orders/{id:N}/invoices", extension, file.ContentType, HttpContext.RequestAborted);
            }

            var receipt = await _service.AttachInvoiceAsync(
                id,
                invoiceNumber,
                file == null ? null : Path.GetFileName(file.FileName),
                storedKey);

            if (receipt == null)
            {
                if (storedKey != null) await _fileStorage.DeleteAsync(storedKey, HttpContext.RequestAborted);

                return NotFound();
            }

            return Ok(receipt);
        }
        catch (ArgumentException ex)
        {
            if (storedKey != null) await _fileStorage.DeleteAsync(storedKey, HttpContext.RequestAborted);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (storedKey != null) await _fileStorage.DeleteAsync(storedKey, HttpContext.RequestAborted);
            return BadRequest(new { message = ex.Message });
        }
        catch
        {
            if (storedKey != null) await _fileStorage.DeleteAsync(storedKey, HttpContext.RequestAborted);
            throw;
        }
    }

    [Authorize(Roles = AppRoles.ReceiptReaders)]
    [HttpGet("{id:guid}/invoice")]
    public async Task<IActionResult> DownloadInvoice(Guid id)
    {
        var invoice = await _service.GetInvoiceAsync(id);

        if (invoice == null)
            return NotFound();

        var stream = await _fileStorage.DownloadAsync(invoice.Value.FilePath, HttpContext.RequestAborted);
        if (stream == null) return NotFound();

        return File(stream, GetInvoiceContentType(Path.GetExtension(invoice.Value.FileName)), invoice.Value.FileName);
    }

    private static void ValidateInvoiceFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new ArgumentException("O arquivo da nota fiscal e obrigatorio.");

        if (file.Length > MaxInvoiceSizeBytes)
            throw new ArgumentException("A nota fiscal deve ter no maximo 10 MB.");

        var extension = Path.GetExtension(file.FileName);

        if (!AllowedInvoiceExtensions.Contains(extension))
            throw new ArgumentException("Apenas arquivos PDF, JPG, JPEG ou PNG sao permitidos para nota fiscal.");

        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !AllowedInvoiceContentTypes.Contains(file.ContentType))
            throw new ArgumentException("O tipo do arquivo da nota fiscal e invalido.");

        var safeName = Path.GetFileName(file.FileName);

        if (safeName != file.FileName)
            throw new ArgumentException("Nome de arquivo invalido.");
    }

    private static string GetInvoiceContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
