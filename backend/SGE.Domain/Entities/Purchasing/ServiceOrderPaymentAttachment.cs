using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;

namespace SGE.Domain.Entities.Purchasing;

public class ServiceOrderPaymentAttachment : BaseEntity
{
    public Guid ServiceOrderPaymentId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public ServiceOrderPayment ServiceOrderPayment { get; private set; } = null!;
    public User UploadedByUser { get; private set; } = null!;

    private ServiceOrderPaymentAttachment() { }

    public ServiceOrderPaymentAttachment(
        Guid serviceOrderPaymentId,
        string originalFileName,
        string filePath,
        string contentType,
        long fileSizeBytes,
        Guid uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(originalFileName) || string.IsNullOrWhiteSpace(filePath) ||
            string.IsNullOrWhiteSpace(contentType) || fileSizeBytes <= 0)
            throw new ArgumentException("Os dados do anexo de pagamento sao invalidos.");

        ServiceOrderPaymentId = serviceOrderPaymentId;
        OriginalFileName = originalFileName;
        FilePath = filePath;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        UploadedByUserId = uploadedByUserId;
        UploadedAt = DateTime.UtcNow;
    }
}
