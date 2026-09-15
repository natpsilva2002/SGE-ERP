namespace SGE.Application.DTOs.Item;

public class ItemDto
{
    public Guid Id { get; set; }

    public Guid? CategoryId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
