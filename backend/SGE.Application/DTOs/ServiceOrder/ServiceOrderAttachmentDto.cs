using SGE.Domain.Enums;

namespace SGE.Application.DTOs.ServiceOrder;

public class ServiceOrderAttachmentDto
{
    public Guid Id { get; set; }

    public Guid ServiceOrderId { get; set; }

    public ServiceOrderAttachmentType Type { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public Guid UploadedByUserId { get; set; }

    public string? UploadedByUserName { get; set; }

    public DateTime UploadedAt { get; set; }
}
