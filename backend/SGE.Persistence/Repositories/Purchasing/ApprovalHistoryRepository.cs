using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class ApprovalHistoryRepository
    : GenericRepository<ApprovalHistory>, IApprovalHistoryRepository
{
    public ApprovalHistoryRepository(SgeDbContext context)
        : base(context)
    {
    }
}