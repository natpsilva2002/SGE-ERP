using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IQuotationItemRepository
{
    Task<IEnumerable<QuotationItem>> GetAllAsync();

    Task<QuotationItem?> GetByIdAsync(Guid id);

    Task AddAsync(QuotationItem quotationItem);

    void Update(QuotationItem quotationItem);

    void Remove(QuotationItem quotationItem);

    Task SaveChangesAsync();
}