using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace SGE.Persistence.Repositories.Purchasing;

public class ServiceOrderPaymentRepository
    : GenericRepository<ServiceOrderPayment>, IServiceOrderPaymentRepository
{
    public ServiceOrderPaymentRepository(SgeDbContext context)
        : base(context)
    {
    }

    public Task<ServiceOrderPayment?> GetByServiceOrderAndIdWithAttachmentsAsync(Guid serviceOrderId, Guid paymentId) =>
        _dbSet.Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.ServiceOrderId == serviceOrderId && x.Id == paymentId);

    public async Task AddAttachmentAsync(ServiceOrderPaymentAttachment attachment) =>
        await _context.Set<ServiceOrderPaymentAttachment>().AddAsync(attachment);

    public void RemoveAttachment(ServiceOrderPaymentAttachment attachment) =>
        _context.Set<ServiceOrderPaymentAttachment>().Remove(attachment);
}
