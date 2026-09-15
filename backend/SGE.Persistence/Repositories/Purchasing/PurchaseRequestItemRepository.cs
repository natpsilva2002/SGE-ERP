using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class PurchaseRequestItemRepository
    : GenericRepository<PurchaseRequestItem>, IPurchaseRequestItemRepository
{
    public PurchaseRequestItemRepository(SgeDbContext context)
        : base(context)
    {
    }
}