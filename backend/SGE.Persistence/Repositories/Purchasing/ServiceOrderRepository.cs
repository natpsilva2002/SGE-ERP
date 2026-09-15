using Microsoft.EntityFrameworkCore;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class ServiceOrderRepository
    : GenericRepository<ServiceOrder>, IServiceOrderRepository
{
    public ServiceOrderRepository(SgeDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<ServiceOrder>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(x => x.PurchaseRequest)
            .Include(x => x.Work)
            .Include(x => x.Supplier)
            .Include(x => x.ContractUploadedByUser)
            .Include(x => x.Measurements)
                .ThenInclude(x => x.CreatedByUser)
            .Include(x => x.Measurements)
                .ThenInclude(x => x.ApprovedByUser)
            .Include(x => x.Measurements)
                .ThenInclude(x => x.RejectedByUser)
            .Include(x => x.Payments)
                .ThenInclude(x => x.PaidByUser)
            .Include(x => x.AdvancePaymentRequests)
                .ThenInclude(x => x.RequestedByUser)
            .Include(x => x.AdvancePaymentRequests)
                .ThenInclude(x => x.ApprovedByUser)
            .Include(x => x.AdvancePaymentRequests)
                .ThenInclude(x => x.RejectedByUser)
            .Include(x => x.AdvancePaymentRequests)
                .ThenInclude(x => x.Payments)
            .Include(x => x.Attachments)
                .ThenInclude(x => x.UploadedByUser)
            .ToListAsync();
    }

    public async Task<ServiceOrder?> GetWithDetailsByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(x => x.PurchaseRequest)
            .Include(x => x.Work)
            .Include(x => x.Supplier)
            .Include(x => x.ContractUploadedByUser)
            .Include(x => x.Measurements)
                .ThenInclude(x => x.CreatedByUser)
            .Include(x => x.Measurements)
                .ThenInclude(x => x.ApprovedByUser)
            .Include(x => x.Measurements)
                .ThenInclude(x => x.RejectedByUser)
            .Include(x => x.Payments)
                .ThenInclude(x => x.PaidByUser)
            .Include(x => x.AdvancePaymentRequests)
                .ThenInclude(x => x.RequestedByUser)
            .Include(x => x.AdvancePaymentRequests)
                .ThenInclude(x => x.ApprovedByUser)
            .Include(x => x.AdvancePaymentRequests)
                .ThenInclude(x => x.RejectedByUser)
            .Include(x => x.AdvancePaymentRequests)
                .ThenInclude(x => x.Payments)
            .Include(x => x.Attachments)
                .ThenInclude(x => x.UploadedByUser)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> ExistsForPurchaseRequestAsync(Guid purchaseRequestId)
    {
        return await _dbSet.AnyAsync(x => x.PurchaseRequestId == purchaseRequestId);
    }

    public async Task<bool> ExistsByNumberAsync(string number)
    {
        return await _dbSet.AnyAsync(x => x.Number == number);
    }
}
