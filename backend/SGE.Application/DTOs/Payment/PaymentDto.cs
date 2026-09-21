using SGE.Domain.Enums;

namespace SGE.Application.DTOs.Payment;

public class PaymentDto
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public Guid PaidByUserId { get; set; }

    public string? PaidByUserName { get; set; }

    public DateTime PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public string? TransactionReference { get; set; }

    public DateTime? DueDate { get; set; }

    public string? InvoiceNumber { get; set; }

    public string? InvoiceFileName { get; set; }

    public string? InvoiceFilePath { get; set; }

    public string? Observation { get; set; }

    public PaymentStatus Status { get; set; }

    public List<PaymentAttachmentDto> Attachments { get; set; } = new();
}
