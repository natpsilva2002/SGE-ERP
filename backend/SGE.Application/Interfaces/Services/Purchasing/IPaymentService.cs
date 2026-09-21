using SGE.Application.DTOs.Payment;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetAllAsync();

    Task<PaymentDto?> GetByIdAsync(Guid id);

    Task<IEnumerable<PaymentDto>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId);

    Task<PaymentDto?> PayAsync(Guid purchaseOrderId, PayPurchaseOrderDto dto);

    Task<PaymentDto?> AddAttachmentAsync(Guid paymentId, string originalFileName, string filePath, string contentType, long fileSizeBytes, Guid uploadedByUserId);
    Task<(string FilePath, string FileName, string ContentType)?> GetAttachmentAsync(Guid paymentId, Guid attachmentId);
    Task<(bool Deleted, string? FilePath)> DeleteAttachmentAsync(Guid paymentId, Guid attachmentId);
}
