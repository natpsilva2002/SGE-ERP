namespace SGE.Application.DTOs.ServiceOrder;

public class CreateServiceOrderDto
{
    public Guid PurchaseRequestId { get; set; }

    public Guid SupplierId { get; set; }

    public decimal ContractedValue { get; set; }

    public decimal ContractedQuantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string? PaymentCondition { get; set; }

    public int? InstallmentCount { get; set; }
}
