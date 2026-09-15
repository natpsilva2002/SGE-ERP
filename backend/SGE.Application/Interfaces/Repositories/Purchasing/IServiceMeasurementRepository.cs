using SGE.Application.Interfaces.Repositories.Base;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Application.Interfaces.Repositories.Purchasing;

public interface IServiceMeasurementRepository : IGenericRepository<ServiceMeasurement>
{
    Task<IEnumerable<ServiceMeasurement>> GetByServiceOrderIdAsync(Guid serviceOrderId);

    Task<ServiceMeasurement?> GetByServiceOrderAndIdAsync(
        Guid serviceOrderId,
        Guid measurementId);

    Task<decimal> SumByServiceOrderAsync(
        Guid serviceOrderId,
        params ServiceMeasurementStatus[] statuses);

    Task<int> CountByServiceOrderAsync(Guid serviceOrderId);
}
