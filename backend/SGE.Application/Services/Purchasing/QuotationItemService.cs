using SGE.Application.DTOs.QuotationItem;
using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Application.Services.Purchasing;

public class QuotationItemService : IQuotationItemService
{
    private readonly IQuotationItemRepository _repository;
    private readonly IQuotationRepository _quotationRepository;
    private readonly IPurchaseRequestItemRepository _purchaseRequestItemRepository;
    private readonly ISupplierRepository _supplierRepository;

    public QuotationItemService(
        IQuotationItemRepository repository,
        IQuotationRepository quotationRepository,
        IPurchaseRequestItemRepository purchaseRequestItemRepository,
        ISupplierRepository supplierRepository)
    {
        _repository = repository;
        _quotationRepository = quotationRepository;
        _purchaseRequestItemRepository = purchaseRequestItemRepository;
        _supplierRepository = supplierRepository;
    }

    public async Task<IEnumerable<QuotationItemDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();

        return items.Select(MapToDto);
    }

    public async Task<QuotationItemDto?> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);

        return item == null
            ? null
            : MapToDto(item);
    }

    public async Task<QuotationItemDto> CreateAsync(
        CreateQuotationItemDto dto)
    {
        var quotation = await _quotationRepository
            .GetByIdAsync(dto.QuotationId);

        if (quotation == null)
            throw new ArgumentException(
                "A cotacao informada nao existe.");

        EnsureQuotationDraft(quotation);

        var purchaseRequestItem = await _purchaseRequestItemRepository
            .GetByIdAsync(dto.PurchaseRequestItemId);

        if (purchaseRequestItem == null)
            throw new ArgumentException(
                "O item da solicitacao de compra informado nao existe.");

        if (purchaseRequestItem.PurchaseRequestId != quotation.PurchaseRequestId)
            throw new InvalidOperationException(
                "O item informado nao pertence a solicitacao vinculada a cotacao.");

        if (purchaseRequestItem.Quantity <= 0)
            throw new InvalidOperationException(
                "A quantidade do item da solicitacao deve ser maior que zero.");

        var supplier = await _supplierRepository
            .GetByIdAsync(dto.SupplierId);

        if (supplier == null)
            throw new ArgumentException(
                "O fornecedor informado nao existe.");

        if (!supplier.IsActive)
            throw new InvalidOperationException(
                "Nao e possivel cadastrar proposta para fornecedor inativo.");

        var totalPrice = GetTotalPrice(dto.TotalPrice, dto.UnitPrice, purchaseRequestItem.Quantity);
        var unitPrice = CalculateUnitPrice(totalPrice, purchaseRequestItem.Quantity);

        var quotationItem = new QuotationItem(
            dto.QuotationId,
            dto.PurchaseRequestItemId,
            dto.SupplierId,
            unitPrice,
            totalPrice,
            dto.DeliveryDays,
            dto.ProposalNumber,
            dto.PaymentCondition,
            dto.InstallmentCount,
            dto.Observation);

        await _repository.AddAsync(quotationItem);
        await _repository.SaveChangesAsync();

        return MapToDto(quotationItem);
    }

    public async Task<QuotationItemDto?> UpdateAsync(
        Guid id,
        UpdateQuotationItemDto dto)
    {
        var item = await _repository.GetByIdAsync(id);

        if (item == null)
            return null;

        var quotation = await _quotationRepository
            .GetByIdAsync(item.QuotationId);

        if (quotation == null)
            throw new ArgumentException(
                "A cotacao informada nao existe.");

        EnsureQuotationDraft(quotation);

        var purchaseRequestItem = await _purchaseRequestItemRepository
            .GetByIdAsync(item.PurchaseRequestItemId);

        if (purchaseRequestItem == null)
            throw new ArgumentException(
                "O item da solicitacao de compra informado nao existe.");

        var totalPrice = GetTotalPrice(dto.TotalPrice, dto.UnitPrice, purchaseRequestItem.Quantity);
        var unitPrice = CalculateUnitPrice(totalPrice, purchaseRequestItem.Quantity);

        item.Update(
            unitPrice,
            totalPrice,
            dto.DeliveryDays,
            dto.ProposalNumber,
            dto.PaymentCondition,
            dto.InstallmentCount,
            dto.Observation);

        _repository.Update(item);
        await _repository.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);

        if (item == null)
            return false;

        var quotation = await _quotationRepository
            .GetByIdAsync(item.QuotationId);

        if (quotation == null)
            throw new ArgumentException(
                "A cotacao informada nao existe.");

        EnsureQuotationDraft(quotation);

        _repository.Remove(item);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<QuotationItemDto?> SelectAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);

        if (item == null)
            return null;

        var quotation = await _quotationRepository
            .GetByIdAsync(item.QuotationId);

        if (quotation == null)
            throw new ArgumentException(
                "A cotacao informada nao existe.");

        if (quotation.Status != QuotationStatus.WaitingApproval)
            throw new InvalidOperationException(
                "Apenas cotacoes aguardando aprovacao permitem selecionar vencedor.");

        var quotationItems = await _repository.GetAllAsync();
        var quotationItemsList = quotationItems
            .Where(x => x.QuotationId == item.QuotationId)
            .ToList();
        var purchaseRequestItems = (await _purchaseRequestItemRepository.FindAsync(
                x => x.PurchaseRequestId == quotation.PurchaseRequestId))
            .ToList();

        var supplierQuotedAllItems = purchaseRequestItems.All(requestItem =>
            quotationItemsList.Any(quotationItem =>
                quotationItem.SupplierId == item.SupplierId &&
                quotationItem.PurchaseRequestItemId == requestItem.Id));

        if (!supplierQuotedAllItems)
            throw new InvalidOperationException(
                "Este fornecedor nao cotou todos os materiais da solicitacao.");

        foreach (var quotationItem in quotationItemsList)
        {
            if (quotationItem.SupplierId == item.SupplierId)
            {
                quotationItem.Select();
            }
            else
            {
                quotationItem.Unselect();
            }
        }

        await _repository.SaveChangesAsync();

        return MapToDto(item);
    }

    private static void EnsureQuotationDraft(Quotation quotation)
    {
        if (quotation.Status != QuotationStatus.Draft)
            throw new InvalidOperationException(
                "Orcamentos so podem ser alterados enquanto a cotacao estiver em rascunho.");
    }

    private static QuotationItemDto MapToDto(
        QuotationItem item)
    {
        return new QuotationItemDto
        {
            Id = item.Id,
            QuotationId = item.QuotationId,
            PurchaseRequestItemId = item.PurchaseRequestItemId,
            SupplierId = item.SupplierId,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice,
            DeliveryDays = item.DeliveryDays,
            ProposalNumber = item.ProposalNumber,
            PaymentCondition = item.PaymentCondition,
            InstallmentCount = item.InstallmentCount,
            Observation = item.Observation,
            Selected = item.Selected
        };
    }

    private static decimal GetTotalPrice(
        decimal totalPrice,
        decimal unitPrice,
        decimal quantity)
    {
        if (totalPrice > 0)
            return totalPrice;

        if (unitPrice > 0)
            return unitPrice * quantity;

        throw new ArgumentException("O preco total deve ser maior que zero.");
    }

    private static decimal CalculateUnitPrice(
        decimal totalPrice,
        decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("A quantidade deve ser maior que zero.");

        return Math.Round(totalPrice / quantity, 4, MidpointRounding.AwayFromZero);
    }
}
