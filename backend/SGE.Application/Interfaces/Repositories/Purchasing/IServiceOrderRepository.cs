using SGE.Application.Interfaces.Repositories.Base;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IServiceOrderRepository : IGenericRepository<ServiceOrder>
{
    Task<IEnumerable<ServiceOrder>> GetAllWithDetailsAsync();

    Task<ServiceOrder?> GetWithDetailsByIdAsync(Guid id);

    Task<bool> ExistsForPurchaseRequestAsync(Guid purchaseRequestId);

    Task<bool> ExistsByNumberAsync(string number);
}
