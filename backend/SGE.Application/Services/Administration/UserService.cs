using SGE.Application.DTOs.User;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Services.Administration;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Domain.Entities.Administration;

namespace SGE.Application.Services.Administration;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IUserRepository repository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _repository.GetAllWithRoleAsync();

        return users.Select(MapToDto);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _repository.GetByIdWithRoleAsync(id);

        if (user == null)
            return null;

        return MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        ValidateUserFields(
            dto.FirstName,
            dto.LastName,
            dto.Email);

        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("A senha inicial deve ser informada.");

        var role = await _roleRepository.GetByIdAsync(dto.RoleId);

        if (role == null)
            throw new ArgumentException("O perfil informado nao existe.");

        var existingUsers = await _repository.FindAsync(
            x => x.Email.ToLower() == dto.Email.Trim().ToLower());

        if (existingUsers.Any())
            throw new ArgumentException("Ja existe usuario cadastrado com este email.");

        var user = new User(
            dto.FirstName.Trim(),
            dto.LastName.Trim(),
            dto.Email.Trim(),
            _passwordHasher.HashPassword(dto.Password),
            dto.RoleId);

        user.UpdateProfile(
            dto.FirstName.Trim(),
            dto.LastName.Trim(),
            dto.Email.Trim(),
            dto.PhoneNumber,
            dto.RoleId,
            dto.IsActive);

        await _repository.AddAsync(user);
        await _repository.SaveChangesAsync();

        return MapToDto(await _repository.GetByIdWithRoleAsync(user.Id) ?? user);
    }

    public async Task<UserDto?> UpdateAsync(
        Guid id,
        UpdateUserDto dto)
    {
        var user = await _repository.GetByIdAsync(id);

        if (user == null)
            return null;

        ValidateUserFields(
            dto.FirstName,
            dto.LastName,
            dto.Email);

        var role = await _roleRepository.GetByIdAsync(dto.RoleId);

        if (role == null)
            throw new ArgumentException("O perfil informado nao existe.");

        var existingUsers = await _repository.FindAsync(
            x => x.Id != id && x.Email.ToLower() == dto.Email.Trim().ToLower());

        if (existingUsers.Any())
            throw new ArgumentException("Ja existe usuario cadastrado com este email.");

        user.UpdateProfile(
            dto.FirstName.Trim(),
            dto.LastName.Trim(),
            dto.Email.Trim(),
            dto.PhoneNumber,
            dto.RoleId,
            dto.IsActive);

        _repository.Update(user);
        await _repository.SaveChangesAsync();

        return MapToDto(await _repository.GetByIdWithRoleAsync(user.Id) ?? user);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _repository.GetByIdAsync(id);

        if (user == null)
            return false;

        user.Deactivate();
        _repository.Update(user);
        await _repository.SaveChangesAsync();

        return true;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            RoleId = user.RoleId,
            Role = user.Role?.Name ?? string.Empty
        };
    }

    private static void ValidateUserFields(
        string firstName,
        string lastName,
        string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("O nome deve ser informado.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("O sobrenome deve ser informado.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("O email deve ser informado.");
    }
}
