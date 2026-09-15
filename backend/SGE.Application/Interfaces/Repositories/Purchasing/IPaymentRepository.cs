using SGE.Application.Interfaces.Repositories.Base;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<IEnumerable<Payment>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId);
}
