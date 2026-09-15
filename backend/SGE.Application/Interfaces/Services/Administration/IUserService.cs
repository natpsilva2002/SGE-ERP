using SGE.Application.DTOs.User;

namespace SGE.Application.Interfaces.Services.Administration;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();

    Task<UserDto?> GetByIdAsync(Guid id);

    Task<UserDto> CreateAsync(CreateUserDto dto);

    Task<UserDto?> UpdateAsync(
        Guid id,
        UpdateUserDto dto);

    Task<bool> DeleteAsync(Guid id);
}