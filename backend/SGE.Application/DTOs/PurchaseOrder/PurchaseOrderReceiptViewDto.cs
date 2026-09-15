using SGE.Domain.Enums;

namespace SGE.Application.DTOs.PurchaseOrder;

public class PurchaseOrderReceiptViewDto
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public PurchaseOrderStatus Status { get; set; }

    public decimal TotalValue { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal AmountPending { get; set; }

    public PurchaseOrderPaymentStatus PaymentStatus { get; set; }

    public IEnumerable<PurchaseOrderReceiptViewItemDto> Items { get; set; } =
        new List<PurchaseOrderReceiptViewItemDto>();
}
