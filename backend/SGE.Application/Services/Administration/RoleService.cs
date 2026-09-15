using SGE.Application.DTOs.Role;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Services.Administration;
using SGE.Domain.Entities.Administration;

namespace SGE.Application.Services.Administration;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _repository;

    public RoleService(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _repository.GetAllAsync();

        return roles.Select(MapToDto);
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id)
    {
        var role = await _repository.GetByIdAsync(id);

        if (role == null)
            return null;

        return MapToDto(role);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
    {
        var role = new Role(
            dto.Name,
            dto.Description);

        await _repository.AddAsync(role);
        await _repository.SaveChangesAsync();

        return MapToDto(role);
    }

    public async Task<RoleDto?> UpdateAsync(
        Guid id,
        UpdateRoleDto dto)
    {
        var role = await _repository.GetByIdAsync(id);

        if (role == null)
            return null;

        role.Update(
            dto.Name,
            dto.Description);

        _repository.Update(role);
        await _repository.SaveChangesAsync();

        return MapToDto(role);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var role = await _repository.GetByIdAsync(id);

        if (role == null)
            return false;

        _repository.Remove(role);
        await _repository.SaveChangesAsync();

        return true;
    }

    private static RoleDto MapToDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        };
    }
}