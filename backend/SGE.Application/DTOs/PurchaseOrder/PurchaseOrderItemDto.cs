namespace SGE.Application.DTOs.PurchaseOrder;

public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public Guid ItemId { get; set; }

    public string ItemDescription { get; set; } = string.Empty;

    public decimal QuantityOrdered { get; set; }

    public decimal QuantityReceived { get; set; }

    public decimal QuantityPending { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal? NegotiatedTotalValue { get; set; }

    public decimal TotalValue { get; set; }

    public string? Observation { get; set; }
}
