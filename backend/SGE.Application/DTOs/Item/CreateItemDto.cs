namespace SGE.Application.DTOs.Item;

public class CreateItemDto
{
    public Guid? CategoryId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
