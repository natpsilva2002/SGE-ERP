using SGE.Domain.Enums;

namespace SGE.Application.DTOs.Quotation;

public class QuotationDto
{
    public Guid Id { get; set; }

    public Guid PurchaseRequestId { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime QuotationDate { get; set; }

    public string? CreatedByUserName { get; set; }

    public string? Observation { get; set; }

    public QuotationStatus Status { get; set; }

    public DateTime? FirstApprovedAt { get; set; }

    public Guid? FirstApprovedByUserId { get; set; }

    public string? FirstApprovedByUserName { get; set; }

    public DateTime? SecondApprovedAt { get; set; }

    public Guid? SecondApprovedByUserId { get; set; }

    public string? SecondApprovedByUserName { get; set; }

    public List<QuotationAttachmentDto> Attachments { get; set; } = new();

    public List<QuotationSupplierOfferDto> SupplierOffers { get; set; } = new();
}
