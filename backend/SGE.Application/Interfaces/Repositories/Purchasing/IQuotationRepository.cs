using SGE.Domain.Entities.Purchasing;
using SGE.Application.Interfaces.Repositories.Base;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IQuotationRepository : IGenericRepository<Quotation>
{
    Task<IEnumerable<Quotation>> GetAllWithApprovalsAsync();

    Task<Quotation?> GetByIdWithApprovalsAsync(Guid id);

    Task<bool> ExistsByNumberAsync(string number);

    Task<bool> ExistsForPurchaseRequestAsync(Guid purchaseRequestId);
}
