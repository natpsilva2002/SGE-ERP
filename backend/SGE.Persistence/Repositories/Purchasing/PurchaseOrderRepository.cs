using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using Microsoft.EntityFrameworkCore;
using SGE.Domain.Enums;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class PurchaseOrderRepository
    : GenericRepository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(SgeDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<PurchaseOrder>> GetAllWithItemsAsync()
    {
        return await _dbSet
            .Include(x => x.Supplier)
            .Include(x => x.Quotation)
                .ThenInclude(x => x.PurchaseRequest)
                    .ThenInclude(x => x.Work)
            .Include(x => x.Quotation)
                .ThenInclude(x => x.PurchaseRequest)
                    .ThenInclude(x => x.RequestedByUser)
            .Include(x => x.FirstApprovedByUser)
            .Include(x => x.SecondApprovedByUser)
            .Include(x => x.PaymentApprovedByUser)
            .Include(x => x.Payments)
                .ThenInclude(x => x.PaidByUser)
            .Include(x => x.Items)
                .ThenInclude(x => x.Item)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetWithItemsByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(x => x.Supplier)
            .Include(x => x.Quotation)
                .ThenInclude(x => x.PurchaseRequest)
                    .ThenInclude(x => x.Work)
            .Include(x => x.Quotation)
                .ThenInclude(x => x.PurchaseRequest)
                    .ThenInclude(x => x.RequestedByUser)
            .Include(x => x.FirstApprovedByUser)
            .Include(x => x.SecondApprovedByUser)
            .Include(x => x.PaymentApprovedByUser)
            .Include(x => x.Payments)
                .ThenInclude(x => x.PaidByUser)
            .Include(x => x.Items)
                .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PurchaseOrder?> GetByNumberWithItemsAsync(string number)
    {
        return await _dbSet
            .Include(x => x.Supplier)
            .Include(x => x.Quotation)
                .ThenInclude(x => x.PurchaseRequest)
                    .ThenInclude(x => x.Work)
            .Include(x => x.Quotation)
                .ThenInclude(x => x.PurchaseRequest)
                    .ThenInclude(x => x.RequestedByUser)
            .Include(x => x.FirstApprovedByUser)
            .Include(x => x.SecondApprovedByUser)
            .Include(x => x.PaymentApprovedByUser)
            .Include(x => x.Payments)
                .ThenInclude(x => x.PaidByUser)
            .Include(x => x.Items)
                .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.Number == number);
    }

    public async Task<bool> ExistsActiveForPurchaseRequestAsync(Guid purchaseRequestId)
    {
        return await _dbSet
            .Include(x => x.Quotation)
            .AnyAsync(x =>
                x.Quotation.PurchaseRequestId == purchaseRequestId &&
                x.Status != PurchaseOrderStatus.Cancelled);
    }

    public async Task<bool> ExistsActiveForQuotationAsync(Guid quotationId)
    {
        return await _dbSet
            .AnyAsync(x =>
                x.QuotationId == quotationId &&
                x.Status != PurchaseOrderStatus.Cancelled);
    }
}
