using SGE.Application.DTOs.ServiceOrder;
using SGE.Domain.Enums;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IServiceOrderService
{
    Task<IEnumerable<ServiceOrderDto>> GetAllAsync();

    Task<ServiceOrderDto?> GetByIdAsync(Guid id);

    Task<ServiceOrderDto> CreateAsync(CreateServiceOrderDto dto);

    Task<ServiceOrderDto?> UpdateContractTermsAsync(Guid id, UpdateServiceOrderContractDto dto);

    Task<ServiceOrderDto?> AttachContractAsync(
        Guid id,
        string originalFileName,
        string storedRelativePath,
        Guid uploadedByUserId);

    Task<ServiceOrderDto?> ReleaseAsync(Guid id);

    Task<ServiceOrderDto?> PayAsync(Guid id, PayServiceOrderDto dto);

    Task<ServiceOrderDto?> RequestAdvancePaymentAsync(Guid id, CreateServiceAdvancePaymentRequestDto dto);

    Task<ServiceOrderDto?> ApproveAdvancePaymentAsync(Guid id, Guid advancePaymentRequestId, Guid approvedByUserId);

    Task<ServiceOrderDto?> RejectAdvancePaymentAsync(
        Guid id,
        Guid advancePaymentRequestId,
        RejectServiceAdvancePaymentRequestDto dto);

    Task<ServiceOrderDto?> AddAttachmentAsync(
        Guid id,
        ServiceOrderAttachmentType type,
        string originalFileName,
        string storedRelativePath,
        string contentType,
        long fileSizeBytes,
        Guid uploadedByUserId);

    Task<(string FilePath, string FileName, string ContentType)?> GetAttachmentAsync(Guid id, Guid attachmentId);

    Task<(bool Deleted, string? FilePath)> DeleteAttachmentAsync(Guid id, Guid attachmentId);

    Task<ServiceOrderDto?> AddPaymentAttachmentAsync(
        Guid serviceOrderId,
        Guid paymentId,
        string originalFileName,
        string storedRelativePath,
        string contentType,
        long fileSizeBytes,
        Guid uploadedByUserId);

    Task<(string FilePath, string FileName, string ContentType)?> GetPaymentAttachmentAsync(Guid serviceOrderId, Guid paymentId, Guid attachmentId);

    Task<(string FilePath, string FileName)?> GetContractAsync(Guid id);
}
