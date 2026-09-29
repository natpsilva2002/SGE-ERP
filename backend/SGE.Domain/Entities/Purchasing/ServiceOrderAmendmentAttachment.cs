using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;

namespace SGE.Domain.Entities.Purchasing;

public class ServiceOrderAmendmentAttachment : BaseEntity
{
    public Guid ServiceOrderAmendmentId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public ServiceOrderAmendment Amendment { get; private set; } = null!;
    public User UploadedByUser { get; private set; } = null!;

    private ServiceOrderAmendmentAttachment() { }

    public ServiceOrderAmendmentAttachment(Guid amendmentId, string fileName, string path,
        string contentType, long size, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(path) ||
            string.IsNullOrWhiteSpace(contentType) || size <= 0)
            throw new ArgumentException("Os dados do anexo do adendo são inválidos.");
        ServiceOrderAmendmentId = amendmentId;
        OriginalFileName = fileName;
        FilePath = path;
        ContentType = contentType;
        FileSizeBytes = size;
        UploadedByUserId = userId;
        UploadedAt = DateTime.UtcNow;
    }
}
