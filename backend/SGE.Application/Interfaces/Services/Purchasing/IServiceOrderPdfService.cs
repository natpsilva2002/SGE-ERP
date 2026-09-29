using SGE.Application.DTOs.ServiceOrder;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IServiceOrderPdfService
{
    byte[] Generate(ServiceOrderDto serviceOrder);
}
