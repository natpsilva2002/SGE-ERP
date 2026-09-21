using SGE.Domain.Common;

namespace SGE.Domain.Entities.Catalog;

public class UnitOfMeasure : BaseSoftDeleteEntity
{
    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsActive => !IsDeleted;

    private UnitOfMeasure()
    {
    }

    public UnitOfMeasure(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public void Update(string code, string description, bool isActive)
    {
        Code = code;
        Description = description;
        SetActive(isActive);
    }

    public void SetActive(bool isActive)
    {
        IsDeleted = !isActive;
        DeletedAt = isActive ? null : DeletedAt ?? DateTime.UtcNow;
    }

    public void Deactivate()
    {
        SetActive(false);
    }
}
