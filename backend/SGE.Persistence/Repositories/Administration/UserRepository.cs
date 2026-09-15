using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Domain.Entities.Administration;
using Microsoft.EntityFrameworkCore;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Administration;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(SgeDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<User>> GetAllWithRoleAsync()
    {
        return await _dbSet
            .Include(x => x.Role)
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync();
    }

    public async Task<User?> GetByIdWithRoleAsync(Guid id)
    {
        return await _dbSet
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<User?> GetByEmailWithRoleAsync(string email)
    {
        return await _dbSet
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email);
    }
}
