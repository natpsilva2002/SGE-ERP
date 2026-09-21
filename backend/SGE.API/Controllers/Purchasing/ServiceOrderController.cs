using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.ServiceOrder;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;
using SGE.Domain.Enums;

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
    private readonly ICurrentUserService _currentUserService;
    private readonly IWebHostEnvironment _environment;

    public ServiceOrderController(
        IServiceOrderService service,
        IServiceMeasurementService measurementService,
        ICurrentUserService currentUserService,
        IWebHostEnvironment environment)
    {
        _service = service;
        _measurementService = measurementService;
        _currentUserService = currentUserService;
        _environment = environment;
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

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpPost("{id:guid}/contract")]
    public async Task<ActionResult<ServiceOrderDto>> UploadContract(
        Guid id,
        IFormFile file)
    {
        try
        {
            ValidateContractFile(file);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var uploadRoot = GetContractUploadRoot();
            Directory.CreateDirectory(uploadRoot);

            var fullPath = Path.Combine(uploadRoot, storedFileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = Path.Combine("uploads", "service-contracts", storedFileName);
            var serviceOrder = await _service.AttachContractAsync(
                id,
                Path.GetFileName(file.FileName),
                relativePath,
                _currentUserService.UserId);

            if (serviceOrder == null)
            {
                System.IO.File.Delete(fullPath);
                return NotFound();
            }

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
    [HttpGet("{id:guid}/contract")]
    public async Task<IActionResult> DownloadContract(Guid id)
    {
        var contract = await _service.GetContractAsync(id);

        if (contract == null)
            return NotFound();

        var fullPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, contract.Value.FilePath));
        var uploadRoot = GetContractUploadRoot();

        if (!fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) ||
            !System.IO.File.Exists(fullPath))
            return NotFound();

        var contentType = GetContentType(Path.GetExtension(fullPath));

        return PhysicalFile(fullPath, contentType, contract.Value.FileName);
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
                var storedFileName = $"{Guid.NewGuid():N}{extension}";
                var uploadRoot = GetAttachmentUploadRoot();
                Directory.CreateDirectory(uploadRoot);

                var fullPath = Path.Combine(uploadRoot, storedFileName);
                await using (var stream = System.IO.File.Create(fullPath))
                {
                    await file.CopyToAsync(stream);
                }

                storedFiles.Add(fullPath);
                var relativePath = Path.Combine("uploads", "service-order-attachments", storedFileName);
                serviceOrder = await _service.AddAttachmentAsync(
                    id,
                    type,
                    Path.GetFileName(file.FileName),
                    relativePath,
                    GetContentType(extension),
                    file.Length,
                    _currentUserService.UserId);

                if (serviceOrder == null)
                    return NotFound();
            }

            return Ok(serviceOrder);
        }
        catch (ArgumentException ex)
        {
            DeleteStoredFiles(storedFiles);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            DeleteStoredFiles(storedFiles);
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId)
    {
        var attachment = await _service.GetAttachmentAsync(id, attachmentId);

        if (attachment == null)
            return NotFound();

        var fullPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, attachment.Value.FilePath));
        var uploadRoot = GetAttachmentUploadRoot();

        if (!fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) ||
            !System.IO.File.Exists(fullPath))
            return NotFound();

        return PhysicalFile(fullPath, attachment.Value.ContentType, attachment.Value.FileName);
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
            var fullPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, result.FilePath));
            var uploadRoot = GetAttachmentUploadRoot();

            if (fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) &&
                System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
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
                var storedFileName = $"{Guid.NewGuid():N}{extension}";
                var uploadRoot = GetPaymentAttachmentUploadRoot();
                Directory.CreateDirectory(uploadRoot);
                var fullPath = Path.Combine(uploadRoot, storedFileName);
                await using (var stream = System.IO.File.Create(fullPath))
                    await file.CopyToAsync(stream);

                storedFiles.Add(fullPath);
                serviceOrder = await _service.AddPaymentAttachmentAsync(
                    id, paymentId, Path.GetFileName(file.FileName),
                    Path.Combine("uploads", "service-payment-attachments", storedFileName),
                    GetContentType(extension), file.Length, _currentUserService.UserId);

                if (serviceOrder == null)
                {
                    DeleteStoredFiles(storedFiles);
                    return NotFound();
                }
            }

            return Ok(serviceOrder);
        }
        catch (ArgumentException ex)
        {
            DeleteStoredFiles(storedFiles);
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/payments/{paymentId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadPaymentAttachment(Guid id, Guid paymentId, Guid attachmentId)
    {
        var attachment = await _service.GetPaymentAttachmentAsync(id, paymentId, attachmentId);
        if (attachment == null)
            return NotFound();

        var fullPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, attachment.Value.FilePath));
        var uploadRoot = GetPaymentAttachmentUploadRoot();
        if (!fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
            return NotFound();

        return PhysicalFile(fullPath, attachment.Value.ContentType, attachment.Value.FileName);
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
                var storedFileName = $"{Guid.NewGuid():N}{extension}";
                var uploadRoot = GetMeasurementAttachmentUploadRoot();
                Directory.CreateDirectory(uploadRoot);
                var fullPath = Path.Combine(uploadRoot, storedFileName);
                await using (var stream = System.IO.File.Create(fullPath))
                    await file.CopyToAsync(stream);

                storedFiles.Add(fullPath);
                measurement = await _measurementService.AddAttachmentAsync(
                    id, measurementId, Path.GetFileName(file.FileName),
                    Path.Combine("uploads", "service-measurement-attachments", storedFileName),
                    GetContentType(extension), file.Length, _currentUserService.UserId);

                if (measurement == null)
                {
                    DeleteStoredFiles(storedFiles);
                    return NotFound();
                }
            }

            return Ok(measurement);
        }
        catch (ArgumentException ex)
        {
            DeleteStoredFiles(storedFiles);
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.FinanceOrAdmin)]
    [HttpGet("{id:guid}/measurements/{measurementId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadMeasurementAttachment(Guid id, Guid measurementId, Guid attachmentId)
    {
        var attachment = await _measurementService.GetAttachmentAsync(id, measurementId, attachmentId);
        if (attachment == null)
            return NotFound();

        var fullPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, attachment.Value.FilePath));
        var uploadRoot = GetMeasurementAttachmentUploadRoot();
        if (!fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
            return NotFound();

        return PhysicalFile(fullPath, attachment.Value.ContentType, attachment.Value.FileName);
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

    private string GetContractUploadRoot()
    {
        return Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath,
            "uploads",
            "service-contracts"));
    }

    private string GetAttachmentUploadRoot()
    {
        return Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath,
            "uploads",
            "service-order-attachments"));
    }

    private string GetPaymentAttachmentUploadRoot() => Path.GetFullPath(Path.Combine(
        _environment.ContentRootPath, "uploads", "service-payment-attachments"));

    private string GetMeasurementAttachmentUploadRoot() => Path.GetFullPath(Path.Combine(
        _environment.ContentRootPath, "uploads", "service-measurement-attachments"));

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

    private static void DeleteStoredFiles(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}
