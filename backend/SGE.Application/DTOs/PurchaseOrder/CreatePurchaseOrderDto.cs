namespace SGE.Application.DTOs.PurchaseOrder;

public class CreatePurchaseOrderDto
{
    public Guid QuotationId { get; set; }

    public Guid SupplierId { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime? ExpectedDeliveryDate { get; set; }
}
