using Microsoft.EntityFrameworkCore;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Base;

namespace SGE.Persistence.Repositories.Purchasing;

public class ServiceMeasurementRepository
    : GenericRepository<ServiceMeasurement>, IServiceMeasurementRepository
{
    public ServiceMeasurementRepository(SgeDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<ServiceMeasurement>> GetByServiceOrderIdAsync(
        Guid serviceOrderId)
    {
        return await WithUsers()
            .Where(x => x.ServiceOrderId == serviceOrderId)
            .OrderBy(x => x.MeasurementNumber)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<ServiceMeasurement?> GetByServiceOrderAndIdAsync(
        Guid serviceOrderId,
        Guid measurementId)
    {
        return await WithUsers()
            .FirstOrDefaultAsync(x =>
                x.ServiceOrderId == serviceOrderId &&
                x.Id == measurementId);
    }

    public async Task<decimal> SumByServiceOrderAsync(
        Guid serviceOrderId,
        params ServiceMeasurementStatus[] statuses)
    {
        return await _dbSet
            .Where(x => x.ServiceOrderId == serviceOrderId &&
                statuses.Contains(x.Status))
            .SumAsync(x => x.Amount);
    }

    public async Task<int> CountByServiceOrderAsync(Guid serviceOrderId)
    {
        return await _dbSet.CountAsync(x => x.ServiceOrderId == serviceOrderId);
    }

    public async Task AddAttachmentAsync(ServiceMeasurementAttachment attachment) =>
        await _context.Set<ServiceMeasurementAttachment>().AddAsync(attachment);

    public void RemoveAttachment(ServiceMeasurementAttachment attachment) =>
        _context.Set<ServiceMeasurementAttachment>().Remove(attachment);

    private IQueryable<ServiceMeasurement> WithUsers()
    {
        return _dbSet
            .Include(x => x.CreatedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.RejectedByUser)
            .Include(x => x.Attachments);
    }
}
