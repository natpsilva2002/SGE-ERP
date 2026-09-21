namespace SGE.Application.DTOs.UnitOfMeasure;

public class UnitOfMeasureDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
