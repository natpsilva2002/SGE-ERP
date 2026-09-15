using SGE.Application.DTOs.ApprovalHistory;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Services.Purchasing;

public class ApprovalHistoryService : IApprovalHistoryService
{
    private readonly IApprovalHistoryRepository _repository;

    public ApprovalHistoryService(IApprovalHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ApprovalHistoryDto>> GetAllAsync()
    {
        var histories = await _repository.GetAllAsync();

        return histories.Select(MapToDto);
    }

    public async Task<ApprovalHistoryDto?> GetByIdAsync(Guid id)
    {
        var history = await _repository.GetByIdAsync(id);

        if (history == null)
            return null;

        return MapToDto(history);
    }

    private static ApprovalHistoryDto MapToDto(ApprovalHistory history)
    {
        return new ApprovalHistoryDto
        {
            Id = history.Id,
            ApprovalId = history.ApprovalId,
            UserId = history.UserId,
            Status = history.Status,
            Observation = history.Observation,
            CreatedAt = history.CreatedAt
        };
    }
}
