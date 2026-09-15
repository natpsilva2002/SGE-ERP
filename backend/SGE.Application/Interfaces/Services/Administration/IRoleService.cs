using SGE.Application.DTOs.Role;

namespace SGE.Application.Interfaces.Services.Administration;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllAsync();

    Task<RoleDto?> GetByIdAsync(Guid id);

    Task<RoleDto> CreateAsync(CreateRoleDto dto);

    Task<RoleDto?> UpdateAsync(
        Guid id,
        UpdateRoleDto dto);

    Task<bool> DeleteAsync(Guid id);
}