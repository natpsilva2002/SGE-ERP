using SGE.Domain.Entities.Purchasing;
using SGE.Application.Interfaces.Repositories.Base;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IPurchaseOrderRepository : IGenericRepository<PurchaseOrder>
{
    Task<IEnumerable<PurchaseOrder>> GetAllWithItemsAsync();

    Task<PurchaseOrder?> GetWithItemsByIdAsync(Guid id);

    Task<PurchaseOrder?> GetByNumberWithItemsAsync(string number);

    Task<bool> ExistsActiveForPurchaseRequestAsync(Guid purchaseRequestId);

    Task<bool> ExistsActiveForQuotationAsync(Guid quotationId);
}
