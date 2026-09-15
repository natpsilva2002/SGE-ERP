using SGE.Domain.Enums;

namespace SGE.Application.DTOs.Payment;

public class PayPurchaseOrderDto
{
    public Guid PaidByUserId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public string? TransactionReference { get; set; }

    public DateTime? DueDate { get; set; }

    public string? InvoiceNumber { get; set; }

    public string? InvoiceFileName { get; set; }

    public string? InvoiceFilePath { get; set; }

    public string? Observation { get; set; }
}
