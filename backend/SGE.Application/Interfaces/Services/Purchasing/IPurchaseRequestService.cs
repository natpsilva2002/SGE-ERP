using SGE.Application.DTOs.PurchaseRequest;
using SGE.Application.DTOs.Approval;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IPurchaseRequestService
{
    Task<IEnumerable<PurchaseRequestDto>> GetAllAsync();

    Task<PurchaseRequestDto?> GetByIdAsync(Guid id);

    Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestDto dto);

    Task<PurchaseRequestDto?> UpdateAsync(
        Guid id,
        UpdatePurchaseRequestDto dto);

    Task<bool> DeleteAsync(Guid id);

    Task<PurchaseRequestDto?> SendToApprovalAsync(Guid id);

    Task<PurchaseRequestDto?> SubmitForApprovalAsync(Guid id);

    Task<PurchaseRequestDto?> SendToQuotationAsync(Guid id);

    Task<PurchaseRequestDto?> ApproveAsync(Guid id, ApprovalDecisionDto dto);

    Task<PurchaseRequestDto?> RejectAsync(Guid id, ApprovalDecisionDto dto);
}
