using SGE.Domain.Enums;

namespace SGE.Application.DTOs.Finance;

public enum FinanceQueueDocumentType
{
    Material = 1,
    Service = 2
}

public enum FinanceQueuePaymentStatus
{
    AwaitingApproval = 1,
    Authorized = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Unpaid = 5
}

public class FinanceQueueItemDto
{
    public Guid Id { get; set; }

    public FinanceQueueDocumentType DocumentType { get; set; }

    public string Number { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string WorkName { get; set; } = string.Empty;

    public decimal TotalValue { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal AmountPending { get; set; }

    public FinanceQueuePaymentStatus PaymentStatus { get; set; }

    public DateTime Date { get; set; }
}
