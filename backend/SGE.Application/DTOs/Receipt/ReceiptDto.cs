using SGE.Domain.Enums;

namespace SGE.Application.DTOs.Receipt;

public class ReceiptDto
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public Guid ReceivedByUserId { get; set; }

    public string? ReceivedByUserName { get; set; }

    public DateTime ReceiptDate { get; set; }

    public string? Observation { get; set; }

    public string? InvoiceNumber { get; set; }

    public string? InvoiceFileName { get; set; }

    public string? InvoiceFilePath { get; set; }

    public ReceiptStatus Status { get; set; }

    public IEnumerable<ReceiptItemDto> Items { get; set; } =
        new List<ReceiptItemDto>();
}
