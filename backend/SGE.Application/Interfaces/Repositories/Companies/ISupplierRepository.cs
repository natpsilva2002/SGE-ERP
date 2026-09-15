using SGE.Domain.Entities.Companies;
using SGE.Application.Interfaces.Repositories.Base;

namespace SGE.Application.Interfaces.Repositories.Companies;

public interface ISupplierRepository : IGenericRepository<Supplier>
{
    Task<bool> ExistsByDocumentAsync(string document, Guid? ignoredId = null);
}
