using SGE.Domain.Common;

namespace SGE.Domain.Entities.Catalog;
public class Category : BaseSoftDeleteEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public ICollection<Item> Items { get; private set; } = new List<Item>();

    private Category()
    {
    }

    public Category(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
    }
}