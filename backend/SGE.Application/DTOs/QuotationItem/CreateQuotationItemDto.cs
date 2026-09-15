namespace SGE.Application.DTOs.QuotationItem;

public class CreateQuotationItemDto
{
    public Guid QuotationId { get; set; }

    public Guid PurchaseRequestItemId { get; set; }

    public Guid SupplierId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public int DeliveryDays { get; set; }

    public string? ProposalNumber { get; set; }

    public string? PaymentCondition { get; set; }

    public int? InstallmentCount { get; set; }

    public string? Observation { get; set; }
}
