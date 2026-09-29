using SGE.Application.DTOs.Finance;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IFinanceQueueService
{
    Task<IEnumerable<FinanceQueueItemDto>> GetAllAsync();
}
