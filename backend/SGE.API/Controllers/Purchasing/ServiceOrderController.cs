using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using SGE.Application.DTOs.ServiceOrder;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;
using SGE.Domain.Enums;
using SGE.API.Services.FileStorage;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceOrderController : ControllerBase
{
    private const long MaxContractSizeBytes = 10 * 1024 * 1024;
    private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx"
    };
    private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };
    private static readonly HashSet<string> AllowedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx"
    };

    private readonly IServiceOrderService _service;
    private readonly IServiceMeasurementService _measurementService;
    private readonly IServiceOrderPdfService _pdfService;
    private readonly IServiceOrderAmendmentService _amendmentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;

    public ServiceOrderController(
        IServiceOrderService service,
        IServiceMeasurementService measurementService,
        IServiceOrderPdfService pdfService,
        IServiceOrderAmendmentService amendmentService,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage)
    {
        _service = service;
        _measurementService = measurementService;
        _pdfService = pdfService;
        _amendmentService = amendmentService;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceOrderDto>>> GetAll()
    {
        var serviceOrders = await _service.GetAllAsync();

        return Ok(serviceOrders);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceOrderDto>> GetById(Guid id)
    {
        var serviceOrder = await _service.GetByIdAsync(id);

        if (serviceOrder == null)
            return NotFound();

        return Ok(serviceOrder);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var serviceOrder = await _service.GetByIdAsync(id);
        if (serviceOrder == null) return NotFound();

        var pdf = _pdfService.Generate(serviceOrder);
        var fileName = Regex.Replace($"OS-{serviceOrder.Number}.pdf", "[<>:\"/\\\\|?*]", "-");
        return File(pdf, "application/pdf", fileName);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<ServiceOrderDto>> Create(
        [FromBody] CreateServiceOrderDto dto)
    {
        try
        {
            var serviceOrder = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = serviceOrder.Id },
                serviceOrder);
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:guid}/contract-terms")]
    public async Task<ActionResult<ServiceOrderDto>> UpdateContractTerms(Guid id, [FromBody] UpdateServiceOrderContractDto dto)
    {
        try
        {
            var order = await _service.UpdateContractTermsAsync(id, dto);
            return order == null ? NotFound() : Ok(order);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/amendments")]
    public async Task<ActionResult<ServiceOrderAmendmentDto>> CreateAmendment(Guid id, [FromBody] CreateServiceOrderAmendmentDto dto)
    {
        try
        {
            var amendment = await _amendmentService.CreateAsync(id, _currentUserService.UserId, dto);
            return CreatedAtAction(nameof(GetById), new { id }, amendment);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:guid}/amendments/{amendmentId:guid}")]
    public async Task<ActionResult<ServiceOrderAmendmentDto>> UpdateAmendment(Guid id, Guid amendmentId, [FromBody] CreateServiceOrderAmendmentDto dto)
    {
        try
        {
            var amendment = await _amendmentService.UpdateAsync(id, amendmentId, dto);
            return amendment == null ? NotFound() : Ok(amendment);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/amendments/{amendmentId:guid}/submit")]
    public async Task<ActionResult<ServiceOrderAmendmentDto>> SubmitAmendment(Guid id, Guid amendmentId)
    {
        try
        {
            var amendment = await _amendmentService.SubmitAsync(id, amendmentId, _currentUserService.UserId);
            return amendment == null ? NotFound() : Ok(amendment);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/amendments/{amendmentId:guid}/approve")]
    public async Task<ActionResult<ServiceOrderAmendmentDto>> ApproveAmendment(Guid id, Guid amendmentId, [FromBody] ServiceOrderAmendmentDecisionDto dto)
    {
        try
        {
            var amendment = await _amendmentService.DecideAsync(id, amendmentId, _currentUserService.UserId, true, dto.Observation);
            return amendment == null ? NotFound() : Ok(amendment);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/amendments/{amendmentId:guid}/reject")]
    public async Task<ActionResult<ServiceOrderAmendmentDto>> RejectAmendment(Guid id, Guid amendmentId, [FromBody] ServiceOrderAmendmentDecisionDto dto)
    {
        try
        {
            var amendment = await _amendmentService.DecideAsync(id, amendmentId, _currentUserService.UserId, false, dto.Observation);
            return amendment == null ? NotFound() : Ok(amendment);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [RequestSizeLimit(80 * 1024 * 1024)]
    [HttpPost("{id:guid}/amendments/{amendmentId:guid}/attachments")]
    public async Task<ActionResult<ServiceOrderAmendmentDto>> UploadAmendmentAttachments(Guid id, Guid amendmentId, [FromForm] List<IFormFile> files)
    {
        var storedFiles = new List<string>();
        try
        {
            if (files == null || files.Count == 0) return BadRequest(new { message = "Informe ao menos um arquivo." });
            ServiceOrderAmendmentDto? result = null;
            foreach (var file in files)
            {
                ValidateDocumentFile(file);
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                await using var stream = file.OpenReadStream();
                var key = await _fileStorage.UploadAsync(stream, $"service-orders/{id:N}/amendments/{amendmentId:N}", extension, GetContentType(extension), HttpContext.RequestAborted);
                storedFiles.Add(key);
                result = await _amendmentService.AddAttachmentAsync(id, amendmentId, Path.GetFileName(file.FileName),
                    key, GetContentType(extension), file.Length, _currentUserService.UserId);
                if (result == null) { await DeleteStoredFilesAsync(storedFiles); return NotFound(); }
            }
            return Ok(result);
        }
        catch (ArgumentException ex) { await DeleteStoredFilesAsync(storedFiles); return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { await DeleteStoredFilesAsync(storedFiles); return BadRequest(new { message = ex.Message }); }
        catch { await DeleteStoredFilesAsync(storedFiles); throw; }
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/amendments/{amendmentId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAmendmentAttachment(Guid id, Guid amendmentId, Guid attachmentId)
    {
        var attachment = await _amendmentService.GetAttachmentAsync(id, amendmentId, attachmentId);
        if (attachment == null) return NotFound();
        var stream = await _fileStorage.DownloadAsync(attachment.Value.FilePath, HttpContext.RequestAborted);
        if (stream == null) return NotFound();
        return File(stream, attachment.Value.ContentType, attachment.Value.FileName);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:guid}/amendments/{amendmentId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAmendmentAttachment(Guid id, Guid amendmentId, Guid attachmentId)
    {
        try
        {
            var result = await _amendmentService.DeleteAttachmentAsync(id, amendmentId, attachmentId);
            if (!result.Deleted) return NotFound();
            if (!string.IsNullOrWhiteSpace(result.FilePath))
            {
                await _fileStorage.DeleteAsync(result.FilePath, HttpContext.RequestAborted);
            }
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpPost("{id:guid}/contract")]
    public async Task<ActionResult<ServiceOrderDto>> UploadContract(
        Guid id,
        IFormFile file)
    {
        string? storedKey = null;
        try
        {
            ValidateContractFile(file);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            await using (var stream = file.OpenReadStream())
                storedKey = await _fileStorage.UploadAsync(stream, $"service-orders/{id:N}/contracts", extension, file.ContentType, HttpContext.RequestAborted);
            var serviceOrder = await _service.AttachContractAsync(
                id,
                Path.GetFileName(file.FileName),
                storedKey,
                _currentUserService.UserId);

            if (serviceOrder == null)
            {
                await _fileStorage.DeleteAsync(storedKey, HttpContext.RequestAborted);
                return NotFound();
            }

            return Ok(serviceOrder);
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

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/contract")]
    public async Task<IActionResult> DownloadContract(Guid id)
    {
        var contract = await _service.GetContractAsync(id);

        if (contract == null)
            return NotFound();

        var stream = await _fileStorage.DownloadAsync(contract.Value.FilePath, HttpContext.RequestAborted);
        if (stream == null) return NotFound();

        return File(stream, GetContentType(Path.GetExtension(contract.Value.FileName)), contract.Value.FileName);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpPost("{id:guid}/release")]
    public async Task<ActionResult<ServiceOrderDto>> Release(Guid id)
    {
        try
        {
            var serviceOrder = await _service.ReleaseAsync(id);

            if (serviceOrder == null)
                return NotFound();

            return Ok(serviceOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpPost("{id:guid}/pay")]
    public async Task<ActionResult<ServiceOrderDto>> Pay(
        Guid id,
        [FromBody] PayServiceOrderDto dto)
    {
        try
        {
            dto.PaidByUserId = _currentUserService.UserId;
            var serviceOrder = await _service.PayAsync(id, dto);

            if (serviceOrder == null)
                return NotFound();

            return Ok(serviceOrder);
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

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpPost("{id:guid}/advance-payments")]
    public async Task<ActionResult<ServiceOrderDto>> RequestAdvancePayment(
        Guid id,
        [FromBody] CreateServiceAdvancePaymentRequestDto dto)
    {
        try
        {
            dto.RequestedByUserId = _currentUserService.UserId;
            var serviceOrder = await _service.RequestAdvancePaymentAsync(id, dto);

            if (serviceOrder == null)
                return NotFound();

            return Ok(serviceOrder);
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/advance-payments/{advancePaymentRequestId:guid}/approve")]
    public async Task<ActionResult<ServiceOrderDto>> ApproveAdvancePayment(
        Guid id,
        Guid advancePaymentRequestId)
    {
        try
        {
            var serviceOrder = await _service.ApproveAdvancePaymentAsync(
                id,
                advancePaymentRequestId,
                _currentUserService.UserId);

            if (serviceOrder == null)
                return NotFound();

            return Ok(serviceOrder);
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/advance-payments/{advancePaymentRequestId:guid}/reject")]
    public async Task<ActionResult<ServiceOrderDto>> RejectAdvancePayment(
        Guid id,
        Guid advancePaymentRequestId,
        [FromBody] RejectServiceAdvancePaymentRequestDto dto)
    {
        try
        {
            dto.RejectedByUserId = _currentUserService.UserId;
            var serviceOrder = await _service.RejectAdvancePaymentAsync(
                id,
                advancePaymentRequestId,
                dto);

            if (serviceOrder == null)
                return NotFound();

            return Ok(serviceOrder);
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

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpPost("{id:guid}/attachments")]
    public async Task<ActionResult<ServiceOrderDto>> UploadAttachments(
        Guid id,
        [FromForm] ServiceOrderAttachmentType type,
        [FromForm] List<IFormFile> files)
    {
        var storedFiles = new List<string>();

        try
        {
            if (files.Count == 0)
                return BadRequest(new { message = "Informe pelo menos um arquivo." });

            ServiceOrderDto? serviceOrder = null;

            foreach (var file in files)
            {
                ValidateAttachmentFile(file);

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                await using var stream = file.OpenReadStream();
                var key = await _fileStorage.UploadAsync(stream, $"service-orders/{id:N}/attachments", extension, GetContentType(extension), HttpContext.RequestAborted);
                storedFiles.Add(key);
                serviceOrder = await _service.AddAttachmentAsync(
                    id,
                    type,
                    Path.GetFileName(file.FileName),
                    key,
                    GetContentType(extension),
                    file.Length,
                    _currentUserService.UserId);

                if (serviceOrder == null)
                {
                    await DeleteStoredFilesAsync(storedFiles);
                    return NotFound();
                }
            }

            return Ok(serviceOrder);
        }
        catch (ArgumentException ex)
        {
            await DeleteStoredFilesAsync(storedFiles);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            await DeleteStoredFilesAsync(storedFiles);
            return BadRequest(new { message = ex.Message });
        }
        catch
        {
            await DeleteStoredFilesAsync(storedFiles);
            throw;
        }
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId)
    {
        var attachment = await _service.GetAttachmentAsync(id, attachmentId);

        if (attachment == null)
            return NotFound();

        var stream = await _fileStorage.DownloadAsync(attachment.Value.FilePath, HttpContext.RequestAborted);
        if (stream == null) return NotFound();

        return File(stream, attachment.Value.ContentType, attachment.Value.FileName);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    {
        var result = await _service.DeleteAttachmentAsync(id, attachmentId);

        if (!result.Deleted)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(result.FilePath))
        {
            await _fileStorage.DeleteAsync(result.FilePath, HttpContext.RequestAborted);
        }

        return NoContent();
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [RequestSizeLimit(80 * 1024 * 1024)]
    [HttpPost("{id:guid}/payments/{paymentId:guid}/attachments")]
    public async Task<ActionResult<ServiceOrderDto>> UploadPaymentAttachments(
        Guid id, Guid paymentId, [FromForm] List<IFormFile> files)
    {
        var storedFiles = new List<string>();
        try
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { message = "Informe ao menos um arquivo." });

            ServiceOrderDto? serviceOrder = null;
            foreach (var file in files)
            {
                ValidateDocumentFile(file);
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                await using var stream = file.OpenReadStream();
                var key = await _fileStorage.UploadAsync(stream, $"service-orders/{id:N}/payments/{paymentId:N}/attachments", extension, GetContentType(extension), HttpContext.RequestAborted);
                storedFiles.Add(key);
                serviceOrder = await _service.AddPaymentAttachmentAsync(
                    id, paymentId, Path.GetFileName(file.FileName),
                    key,
                    GetContentType(extension), file.Length, _currentUserService.UserId);

                if (serviceOrder == null)
                {
                    await DeleteStoredFilesAsync(storedFiles);
                    return NotFound();
                }
            }

            return Ok(serviceOrder);
        }
        catch (ArgumentException ex)
        {
            await DeleteStoredFilesAsync(storedFiles);
            return BadRequest(new { message = ex.Message });
        }
        catch
        {
            await DeleteStoredFilesAsync(storedFiles);
            throw;
        }
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/payments/{paymentId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadPaymentAttachment(Guid id, Guid paymentId, Guid attachmentId)
    {
        var attachment = await _service.GetPaymentAttachmentAsync(id, paymentId, attachmentId);
        if (attachment == null)
            return NotFound();

        var stream = await _fileStorage.DownloadAsync(attachment.Value.FilePath, HttpContext.RequestAborted);
        if (stream == null) return NotFound();

        return File(stream, attachment.Value.ContentType, attachment.Value.FileName);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/measurements")]
    public async Task<ActionResult<IEnumerable<ServiceMeasurementDto>>> GetMeasurements(Guid id)
    {
        var measurements = await _measurementService.GetByServiceOrderIdAsync(id);

        return Ok(measurements);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/measurements/{measurementId:guid}")]
    public async Task<ActionResult<ServiceMeasurementDto>> GetMeasurement(
        Guid id,
        Guid measurementId)
    {
        var measurement = await _measurementService.GetByIdAsync(id, measurementId);

        if (measurement == null)
            return NotFound();

        return Ok(measurement);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [RequestSizeLimit(80 * 1024 * 1024)]
    [HttpPost("{id:guid}/measurements/{measurementId:guid}/attachments")]
    public async Task<ActionResult<ServiceMeasurementDto>> UploadMeasurementAttachments(
        Guid id, Guid measurementId, [FromForm] List<IFormFile> files)
    {
        var storedFiles = new List<string>();
        try
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { message = "Informe ao menos um arquivo." });

            ServiceMeasurementDto? measurement = null;
            foreach (var file in files)
            {
                ValidateDocumentFile(file);
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                await using var stream = file.OpenReadStream();
                var key = await _fileStorage.UploadAsync(stream, $"service-orders/{id:N}/measurements/{measurementId:N}/attachments", extension, GetContentType(extension), HttpContext.RequestAborted);
                storedFiles.Add(key);
                measurement = await _measurementService.AddAttachmentAsync(
                    id, measurementId, Path.GetFileName(file.FileName),
                    key,
                    GetContentType(extension), file.Length, _currentUserService.UserId);

                if (measurement == null)
                {
                    await DeleteStoredFilesAsync(storedFiles);
                    return NotFound();
                }
            }

            return Ok(measurement);
        }
        catch (ArgumentException ex)
        {
            await DeleteStoredFilesAsync(storedFiles);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            await DeleteStoredFilesAsync(storedFiles);
            return BadRequest(new { message = ex.Message });
        }
        catch
        {
            await DeleteStoredFilesAsync(storedFiles);
            throw;
        }
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/measurements/{measurementId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadMeasurementAttachment(Guid id, Guid measurementId, Guid attachmentId)
    {
        var attachment = await _measurementService.GetAttachmentAsync(id, measurementId, attachmentId);
        if (attachment == null)
            return NotFound();

        var stream = await _fileStorage.DownloadAsync(attachment.Value.FilePath, HttpContext.RequestAborted);
        if (stream == null) return NotFound();

        return File(stream, attachment.Value.ContentType, attachment.Value.FileName);
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpPost("{id:guid}/measurements")]
    public async Task<ActionResult<ServiceMeasurementDto>> CreateMeasurement(
        Guid id,
        [FromBody] CreateServiceMeasurementDto dto)
    {
        try
        {
            dto.CreatedByUserId = _currentUserService.UserId;
            var measurement = await _measurementService.CreateAsync(id, dto);

            return CreatedAtAction(
                nameof(GetMeasurement),
                new { id, measurementId = measurement.Id },
                measurement);
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

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpPut("{id:guid}/measurements/{measurementId:guid}")]
    public async Task<ActionResult<ServiceMeasurementDto>> UpdateMeasurement(
        Guid id,
        Guid measurementId,
        [FromBody] UpdateServiceMeasurementDto dto)
    {
        try
        {
            var measurement = await _measurementService.UpdateAsync(id, measurementId, dto);

            if (measurement == null)
                return NotFound();

            return Ok(measurement);
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

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpDelete("{id:guid}/measurements/{measurementId:guid}")]
    public async Task<IActionResult> DeleteMeasurement(Guid id, Guid measurementId)
    {
        try
        {
            var deleted = await _measurementService.DeleteAsync(id, measurementId);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/measurements/{measurementId:guid}/submit")]
    public async Task<ActionResult<ServiceMeasurementDto>> SubmitMeasurement(
        Guid id,
        Guid measurementId)
    {
        try
        {
            var measurement = await _measurementService.SubmitAsync(id, measurementId);

            if (measurement == null)
                return NotFound();

            return Ok(measurement);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/measurements/{measurementId:guid}/approve")]
    public async Task<ActionResult<ServiceMeasurementDto>> ApproveMeasurement(
        Guid id,
        Guid measurementId)
    {
        try
        {
            var measurement = await _measurementService.ApproveAsync(
                id,
                measurementId,
                _currentUserService.UserId);

            if (measurement == null)
                return NotFound();

            return Ok(measurement);
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/measurements/{measurementId:guid}/reject")]
    public async Task<ActionResult<ServiceMeasurementDto>> RejectMeasurement(
        Guid id,
        Guid measurementId,
        [FromBody] RejectServiceMeasurementDto dto)
    {
        try
        {
            dto.RejectedByUserId = _currentUserService.UserId;
            var measurement = await _measurementService.RejectAsync(id, measurementId, dto);

            if (measurement == null)
                return NotFound();

            return Ok(measurement);
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

    private static void ValidateContractFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("O arquivo do contrato e obrigatorio.");

        if (file.Length > MaxContractSizeBytes)
            throw new ArgumentException("O contrato deve ter no maximo 10 MB.");

        var extension = Path.GetExtension(file.FileName);

        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Apenas arquivos PDF, DOC ou DOCX sao permitidos para contrato.");

        var safeName = Path.GetFileName(file.FileName);

        if (safeName != file.FileName)
            throw new ArgumentException("Nome de arquivo invalido.");
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }

    private static void ValidateDocumentFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("O arquivo e obrigatorio.");
        if (file.Length > MaxAttachmentSizeBytes)
            throw new ArgumentException("Cada anexo deve ter no maximo 10 MB.");
        if (!AllowedDocumentExtensions.Contains(Path.GetExtension(file.FileName)))
            throw new ArgumentException("Apenas arquivos PDF, JPG, JPEG, PNG, DOC, DOCX, XLS ou XLSX sao permitidos.");
        if (Path.GetFileName(file.FileName) != file.FileName)
            throw new ArgumentException("Nome de arquivo invalido.");
    }

    private static void ValidateAttachmentFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("O arquivo e obrigatorio.");

        if (file.Length > MaxAttachmentSizeBytes)
            throw new ArgumentException("Cada anexo deve ter no maximo 10 MB.");

        var extension = Path.GetExtension(file.FileName);

        if (!AllowedAttachmentExtensions.Contains(extension))
            throw new ArgumentException("Apenas arquivos PDF, JPG, JPEG ou PNG sao permitidos.");

        var safeName = Path.GetFileName(file.FileName);

        if (safeName != file.FileName)
            throw new ArgumentException("Nome de arquivo invalido.");
    }

    private async Task DeleteStoredFilesAsync(IEnumerable<string> keys)
    {
        foreach (var key in keys)
            await _fileStorage.DeleteAsync(key, HttpContext.RequestAborted);
    }
}
