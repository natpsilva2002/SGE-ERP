using SGE.Domain.Enums;

namespace SGE.Application.DTOs.PurchaseOrder;

public class PurchaseOrderDto
{
    public Guid Id { get; set; }

    public Guid QuotationId { get; set; }

    public Guid SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string SupplierDocument { get; set; } = string.Empty;

    public Guid PurchaseRequestId { get; set; }

    public string PurchaseRequestNumber { get; set; } = string.Empty;

    public Guid WorkId { get; set; }

    public string WorkName { get; set; } = string.Empty;

    public string QuotationNumber { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    public PurchaseOrderStatus Status { get; set; }

    public decimal TotalValue { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal AmountPending { get; set; }

    public PurchaseOrderPaymentStatus PaymentStatus { get; set; }

    public int? DeliveryDays { get; set; }

    public string? PaymentCondition { get; set; }

    public int? InstallmentCount { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTime? FirstApprovedAt { get; set; }

    public Guid? FirstApprovedByUserId { get; set; }

    public string? FirstApprovedByUserName { get; set; }

    public DateTime? SecondApprovedAt { get; set; }

    public Guid? SecondApprovedByUserId { get; set; }

    public string? SecondApprovedByUserName { get; set; }

    public DateTime? SentAt { get; set; }

    public Guid? SentByUserId { get; set; }

    public DateTime? PaymentApprovedAt { get; set; }

    public Guid? PaymentApprovedByUserId { get; set; }

    public string? PaymentApprovedByUserName { get; set; }

    public bool IsPaymentApproved { get; set; }

    public IEnumerable<PurchaseOrderItemDto> Items { get; set; } =
        new List<PurchaseOrderItemDto>();
}
