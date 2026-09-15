using SGE.Domain.Entities.Administration;
using SGE.Application.Interfaces.Repositories.Base;

namespace SGE.Application.Interfaces.Repositories.Administration;

public interface IUserRepository : IGenericRepository<User>
{
    Task<IEnumerable<User>> GetAllWithRoleAsync();

    Task<User?> GetByIdWithRoleAsync(Guid id);

    Task<User?> GetByEmailWithRoleAsync(string email);
}
