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
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .ToListAsync();
    }
}
