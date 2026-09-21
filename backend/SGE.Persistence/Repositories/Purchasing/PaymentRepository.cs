using Microsoft.EntityFrameworkCore;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class PaymentRepository
    : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(SgeDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Payment>> GetByPurchaseOrderIdAsync(
        Guid purchaseOrderId)
    {
        return await _dbSet
            .Include(x => x.Attachments)
            .Include(x => x.PaidByUser)
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetAllWithDetailsAsync() =>
        await _dbSet
            .Include(x => x.Attachments)
            .Include(x => x.PaidByUser)
            .ToListAsync();

    public Task<Payment?> GetByIdWithAttachmentsAsync(Guid id) =>
        _dbSet.Include(x => x.Attachments).Include(x => x.PaidByUser).FirstOrDefaultAsync(x => x.Id == id);

    public async Task AddAttachmentAsync(PaymentAttachment attachment) =>
        await _context.Set<PaymentAttachment>().AddAsync(attachment);

    public void RemoveAttachment(PaymentAttachment attachment) =>
        _context.Set<PaymentAttachment>().Remove(attachment);
}
