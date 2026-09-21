namespace SGE.Application.DTOs.ServiceOrder;

public class ServiceOrderPaymentAttachmentDto
{
    public Guid Id { get; set; }
    public Guid ServiceOrderPaymentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
}
