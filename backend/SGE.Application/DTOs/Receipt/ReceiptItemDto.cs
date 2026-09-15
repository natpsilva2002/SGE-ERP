namespace SGE.Application.DTOs.Receipt;

public class ReceiptItemDto
{
    public Guid Id { get; set; }

    public Guid ReceiptId { get; set; }

    public Guid PurchaseOrderItemId { get; set; }

    public string ItemDescription { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal QuantityReceived { get; set; }

    public string? Observation { get; set; }

    public bool HasDivergence { get; set; }

    public decimal DivergenceQuantity { get; set; }
}
