using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;

namespace SGE.Domain.Entities.Purchasing;

public class ServiceMeasurementAttachment : BaseEntity
{
    public Guid ServiceMeasurementId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public ServiceMeasurement ServiceMeasurement { get; private set; } = null!;
    public User UploadedByUser { get; private set; } = null!;

    private ServiceMeasurementAttachment() { }

    public ServiceMeasurementAttachment(
        Guid serviceMeasurementId,
        string originalFileName,
        string filePath,
        string contentType,
        long fileSizeBytes,
        Guid uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(originalFileName) || string.IsNullOrWhiteSpace(filePath) ||
            string.IsNullOrWhiteSpace(contentType) || fileSizeBytes <= 0)
            throw new ArgumentException("Os dados do anexo de medicao sao invalidos.");

        ServiceMeasurementId = serviceMeasurementId;
        OriginalFileName = originalFileName;
        FilePath = filePath;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        UploadedByUserId = uploadedByUserId;
        UploadedAt = DateTime.UtcNow;
    }
}
