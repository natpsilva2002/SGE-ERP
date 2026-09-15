namespace SGE.Application.DTOs.ServiceOrder;

public class CreateServiceOrderDto
{
    public Guid PurchaseRequestId { get; set; }

    public Guid SupplierId { get; set; }

    public decimal ContractedValue { get; set; }

    public string? PaymentCondition { get; set; }

    public int? InstallmentCount { get; set; }
}
