using SGE.Application.DTOs.PurchaseRequestItem;
using SGE.Application.Interfaces.Repositories.Catalog;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Application.Services.Purchasing;

public class PurchaseRequestItemService : IPurchaseRequestItemService
{
    private readonly IPurchaseRequestItemRepository _repository;
    private readonly IPurchaseRequestRepository _purchaseRequestRepository;
    private readonly IQuotationRepository _quotationRepository;
    private readonly IItemRepository _itemRepository;

    public PurchaseRequestItemService(
        IPurchaseRequestItemRepository repository,
        IPurchaseRequestRepository purchaseRequestRepository,
        IQuotationRepository quotationRepository,
        IItemRepository itemRepository)
    {
        _repository = repository;
        _purchaseRequestRepository = purchaseRequestRepository;
        _quotationRepository = quotationRepository;
        _itemRepository = itemRepository;
    }

    public async Task<IEnumerable<PurchaseRequestItemDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();

        return items.Select(MapToDto);
    }

    public async Task<PurchaseRequestItemDto?> GetByIdAsync(Guid id)
    {
        var purchaseRequestItem = await _repository.GetByIdAsync(id);

        if (purchaseRequestItem == null)
            return null;

        return MapToDto(purchaseRequestItem);
    }

    public async Task<PurchaseRequestItemDto> CreateAsync(CreatePurchaseRequestItemDto dto)
    {
        var purchaseRequest = await _purchaseRequestRepository
            .GetByIdAsync(dto.PurchaseRequestId);

        if (purchaseRequest == null)
            throw new ArgumentException(
                "A solicitacao de compra informada nao existe.");

        await EnsureCanManageMaterialItemsAsync(purchaseRequest, "adicionar");

        var item = await _itemRepository.GetByIdAsync(dto.ItemId);

        if (item == null)
            throw new ArgumentException(
                "O item informado nao existe.");

        if (dto.Quantity <= 0)
            throw new ArgumentException(
                "A quantidade deve ser maior que zero.");

        if (!IsWholeNumber(dto.Quantity))
            throw new ArgumentException(
                "A quantidade de material deve ser um numero inteiro.");

        var purchaseRequestItem = new PurchaseRequestItem(
            dto.PurchaseRequestId,
            dto.ItemId,
            dto.Quantity,
            item.Unit,
            dto.Observation);

        await _repository.AddAsync(purchaseRequestItem);
        await _repository.SaveChangesAsync();

        return MapToDto(purchaseRequestItem);
    }

    public async Task<PurchaseRequestItemDto?> UpdateAsync(
        Guid id,
        UpdatePurchaseRequestItemDto dto)
    {
        var purchaseRequestItem = await _repository.GetByIdAsync(id);

        if (purchaseRequestItem == null)
            return null;

        var purchaseRequest = await _purchaseRequestRepository
            .GetByIdAsync(purchaseRequestItem.PurchaseRequestId);

        if (purchaseRequest == null)
            throw new ArgumentException(
                "A solicitacao de compra informada nao existe.");

        await EnsureCanManageMaterialItemsAsync(purchaseRequest, "alterar");

        if (dto.Quantity <= 0)
            throw new ArgumentException(
                "A quantidade deve ser maior que zero.");

        if (!IsWholeNumber(dto.Quantity))
            throw new ArgumentException(
                "A quantidade de material deve ser um numero inteiro.");

        var item = await _itemRepository.GetByIdAsync(purchaseRequestItem.ItemId);

        if (item == null)
            throw new ArgumentException(
                "O item informado nao existe.");

        purchaseRequestItem.Update(
            dto.Quantity,
            item.Unit,
            dto.Observation);

        _repository.Update(purchaseRequestItem);
        await _repository.SaveChangesAsync();

        return MapToDto(purchaseRequestItem);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var purchaseRequestItem = await _repository.GetByIdAsync(id);

        if (purchaseRequestItem == null)
            return false;

        var purchaseRequest = await _purchaseRequestRepository
            .GetByIdAsync(purchaseRequestItem.PurchaseRequestId);

        if (purchaseRequest == null)
            throw new ArgumentException(
                "A solicitacao de compra informada nao existe.");

        await EnsureCanManageMaterialItemsAsync(purchaseRequest, "remover");

        _repository.Remove(purchaseRequestItem);
        await _repository.SaveChangesAsync();

        return true;
    }

    private async Task EnsureCanManageMaterialItemsAsync(
        PurchaseRequest purchaseRequest,
        string action)
    {
        if (purchaseRequest.Type != PurchaseRequestType.Material)
            throw new InvalidOperationException(
                "Solicitacoes de servico nao utilizam itens de material.");

        if (purchaseRequest.Status == PurchaseRequestStatus.Draft)
            return;

        if (purchaseRequest.Status == PurchaseRequestStatus.WaitingQuotation &&
            !await _quotationRepository.ExistsForPurchaseRequestAsync(purchaseRequest.Id))
            return;

        throw new InvalidOperationException(
            $"So e possivel {action} itens de uma solicitacao de material antes de existir cotacao vinculada.");
    }

    private static bool IsWholeNumber(decimal value)
    {
        return value == decimal.Truncate(value);
    }

    private static PurchaseRequestItemDto MapToDto(PurchaseRequestItem purchaseRequestItem)
    {
        return new PurchaseRequestItemDto
        {
            Id = purchaseRequestItem.Id,
            PurchaseRequestId = purchaseRequestItem.PurchaseRequestId,
            ItemId = purchaseRequestItem.ItemId,
            Quantity = purchaseRequestItem.Quantity,
            Unit = purchaseRequestItem.Unit,
            Observation = purchaseRequestItem.Observation
        };
    }
}
