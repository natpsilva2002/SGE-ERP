using SGE.Application.Interfaces.Repositories.Purchasing;
using Microsoft.EntityFrameworkCore;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class PurchaseRequestRepository
    : GenericRepository<PurchaseRequest>, IPurchaseRequestRepository
{
    public PurchaseRequestRepository(SgeDbContext context)
        : base(context)
    {
    }

    public async Task<bool> ExistsByNumberAsync(string number)
    {
        return await _dbSet.AnyAsync(x => x.Number == number);
    }

    public async Task<IEnumerable<PurchaseRequest>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(x => x.RequestedByUser)
            .ToListAsync();
    }

    public async Task<PurchaseRequest?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(x => x.RequestedByUser)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
