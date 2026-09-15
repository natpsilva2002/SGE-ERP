using Microsoft.EntityFrameworkCore;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class ReceiptRepository
    : GenericRepository<Receipt>, IReceiptRepository
{
    public ReceiptRepository(SgeDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Receipt>> GetAllWithItemsAsync()
    {
        return await _dbSet
            .Include(x => x.ReceivedByUser)
            .Include(x => x.Items)
                .ThenInclude(x => x.PurchaseOrderItem)
                    .ThenInclude(x => x.Item)
            .ToListAsync();
    }

    public async Task<Receipt?> GetWithItemsByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(x => x.ReceivedByUser)
            .Include(x => x.Items)
                .ThenInclude(x => x.PurchaseOrderItem)
                    .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<Receipt>> GetByPurchaseOrderIdWithItemsAsync(Guid purchaseOrderId)
    {
        return await _dbSet
            .Include(x => x.ReceivedByUser)
            .Include(x => x.Items)
                .ThenInclude(x => x.PurchaseOrderItem)
                    .ThenInclude(x => x.Item)
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .OrderByDescending(x => x.ReceiptDate)
            .ToListAsync();
    }
}
