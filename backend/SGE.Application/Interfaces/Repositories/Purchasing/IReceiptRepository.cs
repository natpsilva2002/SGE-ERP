using SGE.Application.Interfaces.Repositories.Base;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IReceiptRepository : IGenericRepository<Receipt>
{
    Task<IEnumerable<Receipt>> GetAllWithItemsAsync();

    Task<Receipt?> GetWithItemsByIdAsync(Guid id);

    Task<IEnumerable<Receipt>> GetByPurchaseOrderIdWithItemsAsync(Guid purchaseOrderId);
}
