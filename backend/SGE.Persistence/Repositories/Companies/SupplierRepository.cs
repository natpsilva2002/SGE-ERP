using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Domain.Entities.Companies;
using Microsoft.EntityFrameworkCore;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Companies;

public class SupplierRepository : GenericRepository<Supplier>, ISupplierRepository
{
    public SupplierRepository(SgeDbContext context)
        : base(context)
    {
    }

    public async Task<bool> ExistsByDocumentAsync(string document, Guid? ignoredId = null)
    {
        return await _dbSet.AnyAsync(x =>
            x.Document == document &&
            (!ignoredId.HasValue || x.Id != ignoredId.Value));
    }
}
