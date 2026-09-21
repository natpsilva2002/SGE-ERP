using SGE.Application.Interfaces.Repositories.Base;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<IEnumerable<Payment>> GetAllWithDetailsAsync();
    Task<IEnumerable<Payment>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId);

    Task<Payment?> GetByIdWithAttachmentsAsync(Guid id);

    Task AddAttachmentAsync(PaymentAttachment attachment);

    void RemoveAttachment(PaymentAttachment attachment);
}
