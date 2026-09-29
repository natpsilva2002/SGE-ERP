using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Approval;
using SGE.Application.DTOs.Quotation;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;
using SGE.API.Services.FileStorage;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotationController : ControllerBase
{
    private readonly IQuotationService _service;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;

    public QuotationController(
        IQuotationService service,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage)
    {
        _service = service;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
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

            QuotationDto? result = null;
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                await using var stream = file.OpenReadStream();
                var key = await _fileStorage.UploadAsync(stream, $"quotation-attachments/quotations/{id:N}/suppliers/{supplierId:N}", extension, file.ContentType, HttpContext.RequestAborted);
                stored.Add(key);
                result = await _service.AddAttachmentAsync(id, supplierId, Path.GetFileName(file.FileName), key, file.ContentType, file.Length, _currentUserService.UserId);
                if (result == null) { await DeleteFilesAsync(stored); return NotFound(); }
            }
            return Ok(result);
        }
        catch (ArgumentException ex) { await DeleteFilesAsync(stored); return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { await DeleteFilesAsync(stored); return BadRequest(new { message = ex.Message }); }
        catch
        {
            await DeleteFilesAsync(stored);
            throw;
        }
    }

    [Authorize(Roles = AppRoles.QuotationReaders)]
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId)
    {
        var attachment = await _service.GetAttachmentAsync(id, attachmentId);
        if (attachment == null) return NotFound();
        var stream = await _fileStorage.DownloadAsync(attachment.Value.FilePath, HttpContext.RequestAborted);
        if (stream == null) return NotFound();
        return File(stream, attachment.Value.ContentType, attachment.Value.FileName);
    }

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    {
        try
        {
            var result = await _service.DeleteAttachmentAsync(id, attachmentId);
            if (!result.Deleted) return NotFound();
            if (result.FilePath != null) await _fileStorage.DeleteAsync(result.FilePath, HttpContext.RequestAborted);
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
    private async Task DeleteFilesAsync(IEnumerable<string> keys)
    {
        foreach (var key in keys)
            await _fileStorage.DeleteAsync(key, HttpContext.RequestAborted);
    }

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

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpPut("{id:guid}/supplier-offers/{supplierId:guid}")]
    public async Task<ActionResult<QuotationDto>> SetSupplierOfferFreight(
        Guid id,
        Guid supplierId,
        [FromBody] UpdateQuotationSupplierOfferDto dto)
    {
        try
        {
            var quotation = await _service.SetSupplierOfferFreightAsync(id, supplierId, dto.FreightValue);
            return quotation == null ? NotFound() : Ok(quotation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpDelete("{id:guid}/supplier-offers/{supplierId:guid}")]
    public async Task<IActionResult> DeleteSupplierOffer(Guid id, Guid supplierId)
    {
        try
        {
            return await _service.DeleteSupplierOfferAsync(id, supplierId)
                ? NoContent()
                : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
