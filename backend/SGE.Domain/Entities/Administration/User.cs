using SGE.Domain.Common;

namespace SGE.Domain.Entities.Administration;

public class User : BaseSoftDeleteEntity
{
    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public bool IsActive { get; private set; } = true;

    public Guid RoleId { get; private set; }

    public Role Role { get; private set; } = null!;

    private User()
    {
    }

    public User(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        Guid roleId)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        RoleId = roleId;
    }

    public void ChangePhone(string phone)
    {
        PhoneNumber = phone;
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        Guid roleId,
        bool isActive)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : phoneNumber;
        RoleId = roleId;
        IsActive = isActive;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
