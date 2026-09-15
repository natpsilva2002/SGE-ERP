using SGE.Domain.Common;

namespace SGE.Domain.Entities.Administration;

public class Role : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public ICollection<User> Users { get; private set; } = new List<User>();

    private Role()
    {
    }

    public Role(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void Update(
        string name,
        string description)
    {
        Name = name;
        Description = description;
    }
}