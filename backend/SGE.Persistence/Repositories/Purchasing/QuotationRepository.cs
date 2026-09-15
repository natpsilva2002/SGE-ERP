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
            .Include(x => x.FirstApprovedByUser)
            .Include(x => x.SecondApprovedByUser)
            .ToListAsync();
    }

    public async Task<Quotation?> GetByIdWithApprovalsAsync(Guid id)
    {
        return await _dbSet
            .Include(x => x.FirstApprovedByUser)
            .Include(x => x.SecondApprovedByUser)
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
}
