using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace SGE.Persistence.Repositories.Purchasing;

public class QuotationRepository
    : GenericRepository<Quotation>, IQuotationRepository
{
    public QuotationRepository(SgeDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Quotation>> GetAllWithApprovalsAsync()
    {
        return await _dbSet
            .Include(x => x.PurchaseRequest)
                .ThenInclude(x => x.RequestedByUser)
            .Include(x => x.FirstApprovedByUser)
            .Include(x => x.SecondApprovedByUser)
            .Include(x => x.Attachments)
            .ToListAsync();
    }

    public async Task<Quotation?> GetByIdWithApprovalsAsync(Guid id)
    {
        return await _dbSet
            .Include(x => x.PurchaseRequest)
                .ThenInclude(x => x.RequestedByUser)
            .Include(x => x.FirstApprovedByUser)
            .Include(x => x.SecondApprovedByUser)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> ExistsByNumberAsync(string number)
    {
        return await _dbSet.AnyAsync(x => x.Number == number);
    }

    public async Task<bool> ExistsForPurchaseRequestAsync(Guid purchaseRequestId)
    {
        return await _dbSet.AnyAsync(x => x.PurchaseRequestId == purchaseRequestId);
    }

    public async Task AddAttachmentAsync(QuotationAttachment attachment) =>
        await _context.Set<QuotationAttachment>().AddAsync(attachment);

    public void RemoveAttachment(QuotationAttachment attachment) =>
        _context.Set<QuotationAttachment>().Remove(attachment);
}
