using SGE.Application.DTOs.ServiceOrder;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IServiceMeasurementService
{
    Task<IEnumerable<ServiceMeasurementDto>> GetByServiceOrderIdAsync(Guid serviceOrderId);

    Task<ServiceMeasurementDto?> GetByIdAsync(Guid serviceOrderId, Guid measurementId);

    Task<ServiceMeasurementDto> CreateAsync(Guid serviceOrderId, CreateServiceMeasurementDto dto);

    Task<ServiceMeasurementDto?> UpdateAsync(
        Guid serviceOrderId,
        Guid measurementId,
        UpdateServiceMeasurementDto dto);

    Task<bool> DeleteAsync(Guid serviceOrderId, Guid measurementId);

    Task<ServiceMeasurementDto?> SubmitAsync(Guid serviceOrderId, Guid measurementId);

    Task<ServiceMeasurementDto?> ApproveAsync(
        Guid serviceOrderId,
        Guid measurementId,
        Guid approvedByUserId);

    Task<ServiceMeasurementDto?> RejectAsync(
        Guid serviceOrderId,
        Guid measurementId,
        RejectServiceMeasurementDto dto);

    Task<ServiceMeasurementDto?> AddAttachmentAsync(
        Guid serviceOrderId,
        Guid measurementId,
        string originalFileName,
        string storedRelativePath,
        string contentType,
        long fileSizeBytes,
        Guid uploadedByUserId);

    Task<(string FilePath, string FileName, string ContentType)?> GetAttachmentAsync(Guid serviceOrderId, Guid measurementId, Guid attachmentId);
}
