using SGE.Application.Interfaces.Repositories.Catalog;
using SGE.Domain.Entities.Catalog;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Catalog;

public class ItemRepository : GenericRepository<Item>, IItemRepository
{
    public ItemRepository(SgeDbContext context)
        : base(context)
    {
    }
}