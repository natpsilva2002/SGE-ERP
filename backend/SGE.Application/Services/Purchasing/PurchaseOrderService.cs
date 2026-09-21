using SGE.Application.DTOs.PurchaseOrder;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Services.Purchasing;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repository;

    public PurchaseOrderService(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetAllAsync()
    {
        var purchaseOrders = await _repository.GetAllWithItemsAsync();

        return purchaseOrders.OrderByDescending(x => x.IssueDate).Select(MapToDto);
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id)
    {
        var purchaseOrder = await _repository.GetWithItemsByIdAsync(id);

        if (purchaseOrder == null)
            return null;

        return MapToDto(purchaseOrder);
    }

    public async Task<PurchaseOrderReceiptViewDto?> GetByNumberAsync(string number)
    {
        var purchaseOrder = await _repository.GetByNumberWithItemsAsync(number);

        if (purchaseOrder == null)
            return null;

        return new PurchaseOrderReceiptViewDto
        {
            Id = purchaseOrder.Id,
            Number = purchaseOrder.Number,
            SupplierId = purchaseOrder.SupplierId,
            SupplierName = purchaseOrder.Supplier.TradeName,
            Status = purchaseOrder.Status,
            TotalValue = purchaseOrder.TotalValue,
            AmountPaid = purchaseOrder.AmountPaid,
            AmountPending = purchaseOrder.AmountPending,
            PaymentStatus = purchaseOrder.PaymentStatus,
            Items = purchaseOrder.Items.Select(x => new PurchaseOrderReceiptViewItemDto
            {
                PurchaseOrderItemId = x.Id,
                ItemId = x.ItemId,
                ItemDescription = x.Item.Description,
                QuantityOrdered = x.QuantityOrdered,
                QuantityReceived = x.QuantityReceived,
                QuantityPending = x.QuantityPending,
                Unit = x.Unit,
                UnitPrice = x.UnitPrice
            })
        };
    }

    public Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto)
    {
        throw new InvalidOperationException(
            "PurchaseOrder deve ser gerada pela aprovacao da cotacao.");
    }

    public async Task<PurchaseOrderDto?> UpdateAsync(
        Guid id,
        UpdatePurchaseOrderDto dto)
    {
        var purchaseOrder = await _repository.GetByIdAsync(id);

        if (purchaseOrder == null)
            return null;

        purchaseOrder.Update(
            dto.Number,
            dto.ExpectedDeliveryDate);

        _repository.Update(purchaseOrder);
        await _repository.SaveChangesAsync();

        return MapToDto(
            await _repository.GetWithItemsByIdAsync(id) ?? purchaseOrder);
    }

    public async Task<PurchaseOrderDto?> ApproveAsync(Guid id, Guid userId)
    {
        var purchaseOrder = await _repository.GetWithItemsByIdAsync(id);

        if (purchaseOrder == null)
            return null;

        throw new InvalidOperationException(
            "A ordem de compra nao possui aprovacao propria. As aprovacoes devem ocorrer na cotacao.");
    }

    public async Task<PurchaseOrderDto?> ApprovePaymentAsync(Guid id, Guid userId)
    {
        var purchaseOrder = await _repository.GetWithItemsByIdAsync(id);

        if (purchaseOrder == null)
            return null;

        purchaseOrder.ApprovePayment(userId);

        _repository.Update(purchaseOrder);
        await _repository.SaveChangesAsync();

        return MapToDto(
            await _repository.GetWithItemsByIdAsync(id) ?? purchaseOrder);
    }

    public async Task<PurchaseOrderDto?> MarkAsSentAsync(Guid id, Guid userId)
    {
        var purchaseOrder = await _repository.GetWithItemsByIdAsync(id);

        if (purchaseOrder == null)
            return null;

        purchaseOrder.MarkAsSent(userId);

        _repository.Update(purchaseOrder);
        await _repository.SaveChangesAsync();

        return MapToDto(purchaseOrder);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var purchaseOrder = await _repository.GetByIdAsync(id);

        if (purchaseOrder == null)
            return false;

        _repository.Remove(purchaseOrder);
        await _repository.SaveChangesAsync();

        return true;
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder purchaseOrder)
    {
        return new PurchaseOrderDto
        {
            Id = purchaseOrder.Id,
            QuotationId = purchaseOrder.QuotationId,
            SupplierId = purchaseOrder.SupplierId,
            SupplierName = purchaseOrder.Supplier?.TradeName ??
                purchaseOrder.Supplier?.CorporateName ??
                string.Empty,
            SupplierDocument = purchaseOrder.Supplier?.Document ?? string.Empty,
            SupplierEmail = purchaseOrder.Supplier?.Email ?? string.Empty,
            SupplierPhone = purchaseOrder.Supplier?.Phone ?? string.Empty,
            PurchaseRequestId = purchaseOrder.Quotation?.PurchaseRequestId ?? Guid.Empty,
            PurchaseRequestNumber = purchaseOrder.Quotation?.PurchaseRequest?.Number ?? string.Empty,
            PurchaseRequestDescription = purchaseOrder.Quotation?.PurchaseRequest?.Description ?? string.Empty,
            WorkId = purchaseOrder.Quotation?.PurchaseRequest?.WorkId ?? Guid.Empty,
            WorkName = purchaseOrder.Quotation?.PurchaseRequest?.Work?.Name ?? string.Empty,
            QuotationNumber = purchaseOrder.Quotation?.Number ?? string.Empty,
            RequestedByUserName = FormatUserName(purchaseOrder.Quotation?.PurchaseRequest?.RequestedByUser),
            Number = purchaseOrder.Number,
            IssueDate = purchaseOrder.IssueDate,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            Status = purchaseOrder.Status,
            TotalValue = purchaseOrder.TotalValue,
            AmountPaid = purchaseOrder.AmountPaid,
            AmountPending = purchaseOrder.AmountPending,
            PaymentStatus = purchaseOrder.PaymentStatus,
            DeliveryDays = purchaseOrder.DeliveryDays,
            PaymentCondition = purchaseOrder.PaymentCondition,
            InstallmentCount = purchaseOrder.InstallmentCount,
            ApprovedAt = purchaseOrder.ApprovedAt,
            ApprovedByUserId = purchaseOrder.ApprovedByUserId,
            FirstApprovedAt = purchaseOrder.FirstApprovedAt,
            FirstApprovedByUserId = purchaseOrder.FirstApprovedByUserId,
            FirstApprovedByUserName = FormatUserName(purchaseOrder.FirstApprovedByUser),
            SecondApprovedAt = purchaseOrder.SecondApprovedAt,
            SecondApprovedByUserId = purchaseOrder.SecondApprovedByUserId,
            SecondApprovedByUserName = FormatUserName(purchaseOrder.SecondApprovedByUser),
            SentAt = purchaseOrder.SentAt,
            SentByUserId = purchaseOrder.SentByUserId,
            PaymentApprovedAt = purchaseOrder.PaymentApprovedAt,
            PaymentApprovedByUserId = purchaseOrder.PaymentApprovedByUserId,
            PaymentApprovedByUserName = FormatUserName(purchaseOrder.PaymentApprovedByUser),
            IsPaymentApproved = purchaseOrder.PaymentApprovedAt.HasValue,
            Items = purchaseOrder.Items.Select(x => new PurchaseOrderItemDto
            {
                Id = x.Id,
                PurchaseOrderId = x.PurchaseOrderId,
                ItemId = x.ItemId,
                ItemDescription = x.Item?.Description ?? string.Empty,
                QuantityOrdered = x.QuantityOrdered,
                QuantityReceived = x.QuantityReceived,
                QuantityPending = x.QuantityPending,
                Unit = x.Unit,
                UnitPrice = x.UnitPrice,
                NegotiatedTotalValue = x.NegotiatedTotalValue,
                TotalValue = x.TotalValue,
                Observation = x.Observation
            })
        };
    }

    private static string? FormatUserName(SGE.Domain.Entities.Administration.User? user)
    {
        if (user == null)
            return null;

        var name = $"{user.FirstName} {user.LastName}".Trim();

        return string.IsNullOrWhiteSpace(name) ? user.Email : name;
    }
}
