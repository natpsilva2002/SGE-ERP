using SGE.Application.DTOs.PurchaseRequestItem;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IPurchaseRequestItemService
{
    Task<IEnumerable<PurchaseRequestItemDto>> GetAllAsync();

    Task<PurchaseRequestItemDto?> GetByIdAsync(Guid id);

    Task<PurchaseRequestItemDto> CreateAsync(
        CreatePurchaseRequestItemDto dto);

    Task<PurchaseRequestItemDto?> UpdateAsync(
        Guid id,
        UpdatePurchaseRequestItemDto dto);

    Task<bool> DeleteAsync(Guid id);
}