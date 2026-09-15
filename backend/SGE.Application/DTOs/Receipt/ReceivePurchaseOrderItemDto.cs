namespace SGE.Application.DTOs.Receipt;

public class ReceivePurchaseOrderItemDto
{
    public Guid PurchaseOrderItemId { get; set; }

    public decimal QuantityReceived { get; set; }

    public string? Observation { get; set; }
}
