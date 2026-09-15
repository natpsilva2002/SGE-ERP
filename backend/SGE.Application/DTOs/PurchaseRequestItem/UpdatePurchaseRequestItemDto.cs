namespace SGE.Application.DTOs.PurchaseRequestItem;

public class UpdatePurchaseRequestItemDto
{
    public decimal Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string? Observation { get; set; }
}