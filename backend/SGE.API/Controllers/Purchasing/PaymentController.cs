using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.DTOs.Payment;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;
using SGE.API.Services.FileStorage;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.PaymentReaders)]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _service;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUserService _currentUserService;

    public PaymentController(IPaymentService service, IFileStorage fileStorage, ICurrentUserService currentUserService)
    {
        _service = service;
        _fileStorage = fileStorage;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAll()
    {
        var payments = await _service.GetAllAsync();

        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetById(Guid id)
    {
        var payment = await _service.GetByIdAsync(id);

        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    [HttpGet("by-purchase-order/{purchaseOrderId:guid}")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetByPurchaseOrder(
        Guid purchaseOrderId)
    {
        var payments = await _service.GetByPurchaseOrderIdAsync(purchaseOrderId);

        return Ok(payments);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [RequestSizeLimit(80 * 1024 * 1024)]
    [HttpPost("{paymentId:guid}/attachments")]
    public async Task<ActionResult<PaymentDto>> UploadAttachments(Guid paymentId, [FromForm] List<IFormFile> files)
    {
        var stored = new List<string>();
        try
        {
            if (files == null || files.Count == 0) return BadRequest(new { message = "Informe ao menos um arquivo." });
            foreach (var file in files)
                ValidateFile(file);

            PaymentDto? result = null;
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                await using var stream = file.OpenReadStream();
                var key = await _fileStorage.UploadAsync(stream, $"payment-attachments/payments/{paymentId:N}", extension, file.ContentType, HttpContext.RequestAborted, file.Length);
                stored.Add(key);
                result = await _service.AddAttachmentAsync(paymentId, Path.GetFileName(file.FileName), key, file.ContentType, file.Length, _currentUserService.UserId);
                if (result == null) { await DeleteFilesAsync(stored); return NotFound(); }
            }
            return Ok(result);
        }
        catch (ArgumentException ex) { await DeleteFilesAsync(stored); return BadRequest(new { message = ex.Message }); }
        catch
        {
            await DeleteFilesAsync(stored);
            throw;
        }
    }

    [HttpGet("{paymentId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid paymentId, Guid attachmentId)
    {
        var attachment = await _service.GetAttachmentAsync(paymentId, attachmentId);
        if (attachment == null) return NotFound();
        var stream = await _fileStorage.DownloadAsync(attachment.Value.FilePath, HttpContext.RequestAborted);
        if (stream == null) return NotFound();
        return File(stream, attachment.Value.ContentType, attachment.Value.FileName);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpDelete("{paymentId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid paymentId, Guid attachmentId)
    {
        var result = await _service.DeleteAttachmentAsync(paymentId, attachmentId);
        if (!result.Deleted) return NotFound();
        if (result.FilePath != null) await _fileStorage.DeleteAsync(result.FilePath, HttpContext.RequestAborted);
        return NoContent();
    }

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };
    private static void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0) throw new ArgumentException("O arquivo nao pode estar vazio.");
        if (file.Length > 10 * 1024 * 1024) throw new ArgumentException("Cada arquivo deve ter no maximo 10 MB.");
        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName))) throw new ArgumentException("Extensao de arquivo nao permitida.");
        if (Path.GetFileName(file.FileName) != file.FileName) throw new ArgumentException("Nome de arquivo invalido.");
    }
    private async Task DeleteFilesAsync(IEnumerable<string> keys)
    {
        foreach (var key in keys)
            await _fileStorage.DeleteAsync(key, HttpContext.RequestAborted);
    }
}
