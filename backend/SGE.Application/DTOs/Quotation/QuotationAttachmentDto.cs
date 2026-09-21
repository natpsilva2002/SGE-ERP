namespace SGE.Application.DTOs.Quotation;

public class QuotationAttachmentDto
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }
    public Guid SupplierId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
}
