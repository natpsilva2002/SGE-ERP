using SGE.Application.Interfaces.Repositories.Base;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IServiceOrderPaymentRepository : IGenericRepository<ServiceOrderPayment>
{
    Task<ServiceOrderPayment?> GetByServiceOrderAndIdWithAttachmentsAsync(Guid serviceOrderId, Guid paymentId);

    Task AddAttachmentAsync(ServiceOrderPaymentAttachment attachment);

    void RemoveAttachment(ServiceOrderPaymentAttachment attachment);
}
