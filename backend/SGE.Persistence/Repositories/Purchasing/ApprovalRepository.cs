using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class ApprovalRepository
    : GenericRepository<Approval>, IApprovalRepository
{
    public ApprovalRepository(SgeDbContext context)
        : base(context)
    {
    }
}