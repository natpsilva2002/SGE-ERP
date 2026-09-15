using SGE.Domain.Entities.Purchasing;
using SGE.Application.Interfaces.Repositories.Base;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IPurchaseRequestRepository : IGenericRepository<PurchaseRequest>
{
    Task<bool> ExistsByNumberAsync(string number);

    Task<IEnumerable<PurchaseRequest>> GetAllWithDetailsAsync();

    Task<PurchaseRequest?> GetByIdWithDetailsAsync(Guid id);
}
