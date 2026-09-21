using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.DTOs.Payment;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.PaymentReaders)]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _service;
    private readonly IWebHostEnvironment _environment;
    private readonly ICurrentUserService _currentUserService;

    public PaymentController(IPaymentService service, IWebHostEnvironment environment, ICurrentUserService currentUserService)
    {
        _service = service;
        _environment = environment;
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

            var root = Path.Combine(_environment.ContentRootPath, "uploads", "payment-attachments");
            Directory.CreateDirectory(root);
            PaymentDto? result = null;
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var storedName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(root, storedName);
                await using (var stream = System.IO.File.Create(fullPath)) await file.CopyToAsync(stream);
                stored.Add(fullPath);
                result = await _service.AddAttachmentAsync(paymentId, Path.GetFileName(file.FileName), Path.Combine("uploads", "payment-attachments", storedName), file.ContentType, file.Length, _currentUserService.UserId);
                if (result == null) { DeleteFiles(stored); return NotFound(); }
            }
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch
        {
            DeleteFiles(stored);
            throw;
        }
    }

    [HttpGet("{paymentId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid paymentId, Guid attachmentId)
    {
        var attachment = await _service.GetAttachmentAsync(paymentId, attachmentId);
        if (attachment == null) return NotFound();
        var path = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, attachment.Value.FilePath));
        var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "uploads", "payment-attachments"));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, attachment.Value.ContentType, attachment.Value.FileName);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpDelete("{paymentId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid paymentId, Guid attachmentId)
    {
        var result = await _service.DeleteAttachmentAsync(paymentId, attachmentId);
        if (!result.Deleted) return NotFound();
        if (result.FilePath != null) DeleteStoredFile(result.FilePath);
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
    private void DeleteStoredFile(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, relativePath));
        var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "uploads", "payment-attachments"));
        if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
    private void DeleteFiles(IEnumerable<string> files) { foreach (var file in files) if (System.IO.File.Exists(file)) System.IO.File.Delete(file); }
}
