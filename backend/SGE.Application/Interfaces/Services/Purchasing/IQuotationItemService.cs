using SGE.Application.DTOs.QuotationItem;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IQuotationItemService
{
    Task<IEnumerable<QuotationItemDto>> GetAllAsync();

    Task<QuotationItemDto?> GetByIdAsync(Guid id);

    Task<QuotationItemDto> CreateAsync(
        CreateQuotationItemDto dto);

    Task<QuotationItemDto?> UpdateAsync(
        Guid id,
        UpdateQuotationItemDto dto);

    Task<bool> DeleteAsync(Guid id);
    Task<QuotationItemDto?> SelectAsync(Guid id);
}