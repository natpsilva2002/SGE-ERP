using Microsoft.EntityFrameworkCore;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class ServiceOrderAmendmentRepository : GenericRepository<ServiceOrderAmendment>, IServiceOrderAmendmentRepository
{
    public ServiceOrderAmendmentRepository(SgeDbContext context) : base(context) { }

    public Task<ServiceOrderAmendment?> GetWithDetailsAsync(Guid amendmentId) => _dbSet
        .Include(x => x.CreatedByUser)
        .Include(x => x.ApprovedByUser)
        .Include(x => x.Attachments)
            .ThenInclude(x => x.UploadedByUser)
        .Include(x => x.Approvals)
        .FirstOrDefaultAsync(x => x.Id == amendmentId);
}
