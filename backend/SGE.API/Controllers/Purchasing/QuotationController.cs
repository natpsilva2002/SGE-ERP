using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Approval;
using SGE.Application.DTOs.Quotation;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;
using Microsoft.AspNetCore.Hosting;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotationController : ControllerBase
{
    private readonly IQuotationService _service;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWebHostEnvironment _environment;

    public QuotationController(
        IQuotationService service,
        ICurrentUserService currentUserService,
        IWebHostEnvironment environment)
    {
        _service = service;
        _currentUserService = currentUserService;
        _environment = environment;
    }

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [RequestSizeLimit(80 * 1024 * 1024)]
    [HttpPost("{id:guid}/attachments")]
    public async Task<ActionResult<QuotationDto>> UploadAttachments(Guid id, [FromForm] Guid supplierId, [FromForm] List<IFormFile> files)
    {
        var stored = new List<string>();
        try
        {
            if (files == null || files.Count == 0) return BadRequest(new { message = "Informe ao menos um arquivo." });
            foreach (var file in files)
                ValidateFile(file);

            var root = Path.Combine(_environment.ContentRootPath, "uploads", "quotation-attachments");
            Directory.CreateDirectory(root);
            QuotationDto? result = null;
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var storedName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(root, storedName);
                await using (var stream = System.IO.File.Create(fullPath)) await file.CopyToAsync(stream);
                stored.Add(fullPath);
                result = await _service.AddAttachmentAsync(id, supplierId, Path.GetFileName(file.FileName), Path.Combine("uploads", "quotation-attachments", storedName), file.ContentType, file.Length, _currentUserService.UserId);
                if (result == null) { DeleteFiles(stored); return NotFound(); }
            }
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch
        {
            DeleteFiles(stored);
            throw;
        }
    }

    [Authorize(Roles = AppRoles.QuotationReaders)]
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId)
    {
        var attachment = await _service.GetAttachmentAsync(id, attachmentId);
        if (attachment == null) return NotFound();
        var path = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, attachment.Value.FilePath));
        var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "uploads", "quotation-attachments"));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, attachment.Value.ContentType, attachment.Value.FileName);
    }

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    {
        try
        {
            var result = await _service.DeleteAttachmentAsync(id, attachmentId);
            if (!result.Deleted) return NotFound();
            if (result.FilePath != null) DeleteStoredFile(result.FilePath);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };
    private static void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0) throw new ArgumentException("O arquivo nao pode estar vazio.");
        if (file.Length > 10 * 1024 * 1024) throw new ArgumentException("Cada arquivo deve ter no maximo 10 MB.");
        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName))) throw new ArgumentException("Extensao de arquivo nao permitida.");
        if (Path.GetFileName(file.FileName) != file.FileName) throw new ArgumentException("Nome de arquivo invalido.");
    }
    private void DeleteStoredFile(string relativePath) { var path = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, relativePath)); var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "uploads", "quotation-attachments")); if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    private static void DeleteFiles(IEnumerable<string> files) { foreach (var file in files) if (System.IO.File.Exists(file)) System.IO.File.Delete(file); }

    [Authorize(Roles = AppRoles.QuotationReaders)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuotationDto>>> GetAll()
    {
        var quotations = await _service.GetAllAsync();

        return Ok(quotations);
    }

    [Authorize(Roles = AppRoles.QuotationReaders)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationDto>> GetById(Guid id)
    {
        var quotation = await _service.GetByIdAsync(id);

        if (quotation == null)
            return NotFound();

        return Ok(quotation);
    }

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpPost]
    public async Task<ActionResult<QuotationDto>> Create(
        [FromBody] CreateQuotationDto dto)
    {
        try
        {
            var quotation = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = quotation.Id },
                quotation);
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

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuotationDto>> Update(
        Guid id,
        [FromBody] UpdateQuotationDto dto)
    {
        try
        {
            var quotation = await _service.UpdateAsync(id, dto);

            if (quotation == null)
                return NotFound();

            return Ok(quotation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<ActionResult<QuotationDto>> SubmitForApproval(Guid id)
    {
        try
        {
            var quotation = await _service.SubmitForApprovalAsync(id);

            if (quotation == null)
                return NotFound();

            return Ok(quotation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPatch("{id:guid}/select-supplier/{supplierId:guid}")]
    public async Task<ActionResult<QuotationDto>> SelectSupplier(
        Guid id,
        Guid supplierId)
    {
        try
        {
            var quotation = await _service.SelectSupplierAsync(id, supplierId);

            if (quotation == null)
                return NotFound();

            return Ok(quotation);
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<QuotationApprovalResultDto>> Approve(
        Guid id,
        [FromBody] ApprovalDecisionDto dto)
    {
        try
        {
            dto.UserId = _currentUserService.UserId;
            var quotation = await _service.ApproveAsync(id, dto);

            if (quotation == null)
                return NotFound();

            return Ok(quotation);
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<QuotationDto>> Reject(
        Guid id,
        [FromBody] ApprovalDecisionDto dto)
    {
        try
        {
            dto.UserId = _currentUserService.UserId;
            var quotation = await _service.RejectAsync(id, dto);

            if (quotation == null)
                return NotFound();

            return Ok(quotation);
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
