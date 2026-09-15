using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Domain.Entities.Companies;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Companies;

public class CompanyRepository : GenericRepository<Company>, ICompanyRepository
{
    public CompanyRepository(SgeDbContext context)
        : base(context)
    {
    }
}