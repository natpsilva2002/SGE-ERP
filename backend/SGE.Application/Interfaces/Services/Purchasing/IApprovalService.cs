using SGE.Application.DTOs.Approval;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IApprovalService
{
    Task<IEnumerable<ApprovalDto>> GetAllAsync();

    Task<ApprovalDto?> GetByIdAsync(Guid id);

    Task<ApprovalDto> CreateAsync(CreateApprovalDto dto);

    Task<ApprovalDto?> UpdateAsync(
        Guid id,
        UpdateApprovalDto dto);

    Task<bool> DeleteAsync(Guid id);
}