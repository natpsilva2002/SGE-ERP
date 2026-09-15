using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class ServiceOrderAttachmentRepository
    : GenericRepository<ServiceOrderAttachment>, IServiceOrderAttachmentRepository
{
    public ServiceOrderAttachmentRepository(SgeDbContext context)
        : base(context)
    {
    }
}
