namespace SGE.Application.DTOs.PurchaseOrder;

public class UpdatePurchaseOrderDto
{
    public string Number { get; set; } = string.Empty;

    public DateTime? ExpectedDeliveryDate { get; set; }
}