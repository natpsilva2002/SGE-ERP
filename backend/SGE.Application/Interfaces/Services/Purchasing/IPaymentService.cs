using SGE.Application.DTOs.Payment;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetAllAsync();

    Task<PaymentDto?> GetByIdAsync(Guid id);

    Task<IEnumerable<PaymentDto>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId);

    Task<PaymentDto?> PayAsync(Guid purchaseOrderId, PayPurchaseOrderDto dto);
}
