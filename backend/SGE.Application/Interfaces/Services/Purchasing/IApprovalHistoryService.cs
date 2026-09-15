using SGE.Application.DTOs.ApprovalHistory;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IApprovalHistoryService
{
    Task<IEnumerable<ApprovalHistoryDto>> GetAllAsync();

    Task<ApprovalHistoryDto?> GetByIdAsync(Guid id);
}
