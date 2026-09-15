namespace SGE.Domain.Common;

public abstract class BaseSoftDeleteEntity : BaseAuditableEntity
{
    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }
} 