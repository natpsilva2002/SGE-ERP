using SGE.Domain.Common;

namespace SGE.Domain.Entities.Catalog;
public class Item : BaseSoftDeleteEntity
{
    public Guid? CategoryId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Unit { get; private set; } = string.Empty;

    public Guid? UnitOfMeasureId { get; private set; }

    public UnitOfMeasure? UnitOfMeasure { get; private set; }

    public bool IsActive { get; private set; } = true;

    public Category? Category { get; private set; }

    private Item()
    {
    }

    public Item(
        Guid? categoryId,
        string code,
        string description,
        string unit,
        bool isActive = true,
        Guid? unitOfMeasureId = null)
    {
        CategoryId = categoryId;
        Code = code;
        Description = description;
        Unit = unit;
        UnitOfMeasureId = unitOfMeasureId;
        IsActive = isActive;
    }

    public void Update(
        string code,
        string description,
        string unit,
        bool isActive,
        Guid? unitOfMeasureId = null)
    {
        Code = code;
        Description = description;
        Unit = unit;
        UnitOfMeasureId = unitOfMeasureId;
        IsActive = isActive;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
