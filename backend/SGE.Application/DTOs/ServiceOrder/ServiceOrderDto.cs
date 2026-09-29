using SGE.Domain.Enums;

namespace SGE.Application.DTOs.ServiceOrder;

public class ServiceOrderDto
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Number { get; set; } = string.Empty;

    public Guid PurchaseRequestId { get; set; }

    public string PurchaseRequestNumber { get; set; } = string.Empty;

    public string? RequestedByUserName { get; set; }

    public Guid WorkId { get; set; }

    public string WorkName { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string SupplierDocument { get; set; } = string.Empty;

    public string SupplierEmail { get; set; } = string.Empty;

    public string SupplierPhone { get; set; } = string.Empty;

    public string ServiceDescription { get; set; } = string.Empty;

    public string? ServiceSpecification { get; set; }

    public decimal? EstimatedQuantity { get; set; }

    public string? Unit { get; set; }

    public decimal ContractedValue { get; set; }

    public decimal CurrentContractedValue { get; set; }

    public decimal? CurrentContractedQuantity { get; set; }

    public bool IsReleasedForExecution { get; set; }

    public string? PaymentCondition { get; set; }

    public int? InstallmentCount { get; set; }

    public ServiceOrderExecutionStatus ExecutionStatus { get; set; }

    public ServiceOrderPaymentStatus PaymentStatus { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal AmountPending { get; set; }

    public decimal AvailableToPay { get; set; }

    public decimal AvailableMeasuredToPay { get; set; }

    public decimal AvailableAdvanceToPay { get; set; }

    public decimal UnmeasuredBalance { get; set; }

    public string? ContractFileName { get; set; }

    public DateTime? ContractUploadedAt { get; set; }

    public Guid? ContractUploadedByUserId { get; set; }

    public string? ContractUploadedByUserName { get; set; }

    public decimal ApprovedMeasuredAmount { get; set; }

    public decimal CommittedMeasuredAmount { get; set; }

    public decimal RemainingToMeasure { get; set; }

    public decimal ExecutionPercentage { get; set; }

    public decimal MeasuredQuantity { get; set; }

    public decimal RemainingQuantity { get; set; }

    public decimal PhysicalPercentage { get; set; }

    public decimal FinancialPercentage { get; set; }

    public IEnumerable<ServiceMeasurementDto> Measurements { get; set; } =
        Enumerable.Empty<ServiceMeasurementDto>();

    public IEnumerable<ServiceOrderPaymentDto> Payments { get; set; } =
        Enumerable.Empty<ServiceOrderPaymentDto>();

    public Guid? LastPaymentId { get; set; }

    public IEnumerable<ServiceAdvancePaymentRequestDto> AdvancePaymentRequests { get; set; } =
        Enumerable.Empty<ServiceAdvancePaymentRequestDto>();

    public IEnumerable<ServiceOrderAttachmentDto> Attachments { get; set; } =
        Enumerable.Empty<ServiceOrderAttachmentDto>();

    public IEnumerable<ServiceOrderAmendmentDto> Amendments { get; set; } =
        Enumerable.Empty<ServiceOrderAmendmentDto>();
}
