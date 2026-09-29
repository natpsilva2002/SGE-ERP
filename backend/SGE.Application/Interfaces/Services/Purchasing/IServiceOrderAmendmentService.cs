using SGE.Application.DTOs.ServiceOrder;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IServiceOrderAmendmentService
{
    Task<ServiceOrderAmendmentDto> CreateAsync(Guid orderId, Guid userId, CreateServiceOrderAmendmentDto dto);
    Task<ServiceOrderAmendmentDto?> UpdateAsync(Guid orderId, Guid amendmentId, CreateServiceOrderAmendmentDto dto);
    Task<ServiceOrderAmendmentDto?> SubmitAsync(Guid orderId, Guid amendmentId, Guid userId);
    Task<ServiceOrderAmendmentDto?> DecideAsync(Guid orderId, Guid amendmentId, Guid userId, bool approve, string? observation);
    Task<ServiceOrderAmendmentDto?> AddAttachmentAsync(Guid orderId, Guid amendmentId, string name, string path, string contentType, long size, Guid userId);
    Task<(bool Deleted, string? FilePath)> DeleteAttachmentAsync(Guid orderId, Guid amendmentId, Guid attachmentId);
    Task<(string FilePath, string FileName, string ContentType)?> GetAttachmentAsync(Guid orderId, Guid amendmentId, Guid attachmentId);
}
