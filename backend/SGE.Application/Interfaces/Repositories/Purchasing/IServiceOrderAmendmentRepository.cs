using SGE.Application.Interfaces.Repositories.Base;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IServiceOrderAmendmentRepository : IGenericRepository<ServiceOrderAmendment>
{
    Task<ServiceOrderAmendment?> GetWithDetailsAsync(Guid amendmentId);
}
