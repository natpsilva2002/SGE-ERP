using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class ServiceOrderAttachment : BaseEntity
{
    public Guid ServiceOrderId { get; private set; }

    public ServiceOrderAttachmentType Type { get; private set; }

    public string OriginalFileName { get; private set; } = string.Empty;

    public string FilePath { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long FileSizeBytes { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public DateTime UploadedAt { get; private set; }

    public ServiceOrder ServiceOrder { get; private set; } = null!;

    public User UploadedByUser { get; private set; } = null!;

    private ServiceOrderAttachment()
    {
    }

    public ServiceOrderAttachment(
        Guid serviceOrderId,
        ServiceOrderAttachmentType type,
        string originalFileName,
        string filePath,
        string contentType,
        long fileSizeBytes,
        Guid uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("O nome do arquivo e obrigatorio.");

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("O caminho do arquivo e obrigatorio.");

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("O tipo do arquivo e obrigatorio.");

        if (fileSizeBytes <= 0)
            throw new ArgumentException("O arquivo deve possuir conteudo.");

        ServiceOrderId = serviceOrderId;
        Type = type;
        OriginalFileName = originalFileName;
        FilePath = filePath;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        UploadedByUserId = uploadedByUserId;
        UploadedAt = DateTime.UtcNow;
    }
}
