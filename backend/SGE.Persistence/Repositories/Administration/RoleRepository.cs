using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Domain.Entities.Administration;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Administration;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(SgeDbContext context)
        : base(context)
    {
    }
}