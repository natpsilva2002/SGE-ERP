using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Entities.Companies;

namespace SGE.Domain.Entities.Purchasing;

public class QuotationAttachment : BaseEntity
{
    public Guid QuotationId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Quotation Quotation { get; private set; } = null!;
    public Supplier Supplier { get; private set; } = null!;
    public User UploadedByUser { get; private set; } = null!;

    private QuotationAttachment() { }

    public QuotationAttachment(Guid quotationId, Guid supplierId, string originalFileName,
        string filePath, string contentType, long fileSizeBytes, Guid uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(originalFileName) || string.IsNullOrWhiteSpace(filePath) ||
            string.IsNullOrWhiteSpace(contentType) || fileSizeBytes <= 0)
            throw new ArgumentException("Os dados do anexo de orcamento sao invalidos.");

        QuotationId = quotationId;
        SupplierId = supplierId;
        OriginalFileName = originalFileName;
        FilePath = filePath;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        UploadedByUserId = uploadedByUserId;
        UploadedAt = DateTime.UtcNow;
    }
}
