namespace SGE.Application.DTOs.Item;

public class UpdateItemDto
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public Guid? UnitOfMeasureId { get; set; }

    public bool IsActive { get; set; } = true;
}
