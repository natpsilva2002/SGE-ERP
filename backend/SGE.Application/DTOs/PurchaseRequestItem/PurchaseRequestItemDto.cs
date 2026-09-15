namespace SGE.Application.DTOs.PurchaseRequestItem;

public class PurchaseRequestItemDto
{
    public Guid Id { get; set; }

    public Guid PurchaseRequestId { get; set; }

    public Guid ItemId { get; set; }

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string? Observation { get; set; }
}