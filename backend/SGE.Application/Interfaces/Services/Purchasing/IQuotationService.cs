using SGE.Application.DTOs.Quotation;
using SGE.Application.DTOs.Approval;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IQuotationService
{
    Task<IEnumerable<QuotationDto>> GetAllAsync();

    Task<QuotationDto?> GetByIdAsync(Guid id);

    Task<QuotationDto> CreateAsync(CreateQuotationDto dto);

    Task<QuotationDto?> UpdateAsync(
        Guid id,
        UpdateQuotationDto dto);

    Task<QuotationDto?> SetSupplierOfferFreightAsync(
        Guid quotationId,
        Guid supplierId,
        decimal freightValue);

    Task<bool> DeleteSupplierOfferAsync(Guid quotationId, Guid supplierId);

    Task<bool> DeleteAsync(Guid id);

    Task<QuotationDto?> SubmitForApprovalAsync(Guid id);

    Task<QuotationDto?> SelectSupplierAsync(Guid quotationId, Guid supplierId);

    Task<QuotationApprovalResultDto?> ApproveAsync(Guid id, ApprovalDecisionDto dto);

    Task<QuotationDto?> RejectAsync(Guid id, ApprovalDecisionDto dto);

    Task<QuotationDto?> AddAttachmentAsync(Guid quotationId, Guid supplierId, string originalFileName, string filePath, string contentType, long fileSizeBytes, Guid uploadedByUserId);
    Task<(string FilePath, string FileName, string ContentType)?> GetAttachmentAsync(Guid quotationId, Guid attachmentId);
    Task<(bool Deleted, string? FilePath)> DeleteAttachmentAsync(Guid quotationId, Guid attachmentId);
}
