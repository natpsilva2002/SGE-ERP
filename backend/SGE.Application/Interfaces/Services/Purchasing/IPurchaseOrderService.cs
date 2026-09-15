using SGE.Application.DTOs.PurchaseOrder;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IPurchaseOrderService
{
    Task<IEnumerable<PurchaseOrderDto>> GetAllAsync();

    Task<PurchaseOrderDto?> GetByIdAsync(Guid id);

    Task<PurchaseOrderReceiptViewDto?> GetByNumberAsync(string number);

    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto);

    Task<PurchaseOrderDto?> UpdateAsync(
        Guid id,
        UpdatePurchaseOrderDto dto);

    Task<PurchaseOrderDto?> ApproveAsync(Guid id, Guid userId);

    Task<PurchaseOrderDto?> ApprovePaymentAsync(Guid id, Guid userId);

    Task<PurchaseOrderDto?> MarkAsSentAsync(Guid id, Guid userId);

    Task<bool> DeleteAsync(Guid id);
}
