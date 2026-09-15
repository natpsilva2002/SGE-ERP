using SGE.Application.DTOs.Receipt;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Services.Purchasing;

public class ReceiptService : IReceiptService
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUserRepository _userRepository;

    public ReceiptService(
        IReceiptRepository receiptRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IUserRepository userRepository)
    {
        _receiptRepository = receiptRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<ReceiptDto>> GetAllAsync()
    {
        var receipts = await _receiptRepository.GetAllWithItemsAsync();

        return receipts.Select(MapToDto);
    }

    public async Task<ReceiptDto?> GetByIdAsync(Guid id)
    {
        var receipt = await _receiptRepository.GetWithItemsByIdAsync(id);

        if (receipt == null)
            return null;

        return MapToDto(receipt);
    }

    public async Task<IEnumerable<ReceiptDto>> GetByPurchaseOrderIdAsync(
        Guid purchaseOrderId)
    {
        var receipts = await _receiptRepository
            .GetByPurchaseOrderIdWithItemsAsync(purchaseOrderId);

        return receipts.Select(MapToDto);
    }

    public async Task<ReceiptDto?> ReceiveAsync(
        Guid purchaseOrderId,
        ReceivePurchaseOrderDto dto)
    {
        var purchaseOrder = await _purchaseOrderRepository
            .GetWithItemsByIdAsync(purchaseOrderId);

        if (purchaseOrder == null)
            return null;

        var user = await _userRepository.GetByIdAsync(dto.ReceivedByUserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        purchaseOrder.EnsureCanReceive();

        if (!purchaseOrder.Items.Any())
            throw new InvalidOperationException(
                "Nao e possivel receber uma ordem de compra sem itens.");

        var receivedItems = dto.Items.ToList();

        if (!receivedItems.Any())
            throw new InvalidOperationException(
                "Informe ao menos um item recebido.");

        if (receivedItems
            .GroupBy(x => x.PurchaseOrderItemId)
            .Any(x => x.Count() > 1))
            throw new InvalidOperationException(
                "Nao informe o mesmo item da ordem de compra mais de uma vez no mesmo recebimento.");

        var receipt = new Receipt(
            purchaseOrder.Id,
            dto.ReceivedByUserId,
            dto.Observation);

        foreach (var receivedItem in receivedItems)
        {
            if (receivedItem.QuantityReceived <= 0)
                throw new ArgumentException(
                    "A quantidade recebida deve ser maior que zero.");

            var purchaseOrderItem = purchaseOrder.Items
                .FirstOrDefault(x => x.Id == receivedItem.PurchaseOrderItemId);

            if (purchaseOrderItem == null)
                throw new InvalidOperationException(
                    "O item informado nao pertence a ordem de compra.");

            var accumulatedQuantity =
                purchaseOrderItem.QuantityReceived + receivedItem.QuantityReceived;

            var divergenceQuantity =
                Math.Max(accumulatedQuantity - purchaseOrderItem.QuantityOrdered, 0);

            var hasDivergence = divergenceQuantity > 0;
            var observation = receivedItem.Observation ?? dto.Observation;

            receipt.AddItem(
                purchaseOrderItem.Id,
                receivedItem.QuantityReceived,
                observation,
                hasDivergence,
                divergenceQuantity);

            purchaseOrderItem.Receive(receivedItem.QuantityReceived);
        }

        purchaseOrder.RefreshReceiptStatus();

        await _receiptRepository.AddAsync(receipt);
        _purchaseOrderRepository.Update(purchaseOrder);
        await _receiptRepository.SaveChangesAsync();

        return MapToDto(receipt);
    }

    public async Task<ReceiptDto?> AttachInvoiceAsync(
        Guid id,
        string? invoiceNumber,
        string? originalFileName,
        string? storedRelativePath)
    {
        var receipt = await _receiptRepository.GetWithItemsByIdAsync(id);

        if (receipt == null)
            return null;

        receipt.AttachInvoice(
            invoiceNumber,
            originalFileName,
            storedRelativePath);

        _receiptRepository.Update(receipt);
        await _receiptRepository.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<(string FilePath, string FileName)?> GetInvoiceAsync(Guid id)
    {
        var receipt = await _receiptRepository.GetWithItemsByIdAsync(id);

        if (receipt == null ||
            string.IsNullOrWhiteSpace(receipt.InvoiceFilePath) ||
            string.IsNullOrWhiteSpace(receipt.InvoiceFileName))
            return null;

        return (receipt.InvoiceFilePath, receipt.InvoiceFileName);
    }

    private static ReceiptDto MapToDto(Receipt receipt)
    {
        return new ReceiptDto
        {
            Id = receipt.Id,
            PurchaseOrderId = receipt.PurchaseOrderId,
            ReceivedByUserId = receipt.ReceivedByUserId,
            ReceivedByUserName = FormatUserName(receipt.ReceivedByUser),
            ReceiptDate = receipt.ReceiptDate,
            Observation = receipt.Observation,
            InvoiceNumber = receipt.InvoiceNumber,
            InvoiceFileName = receipt.InvoiceFileName,
            InvoiceFilePath = receipt.InvoiceFilePath,
            Status = receipt.Status,
            Items = receipt.Items.Select(x => new ReceiptItemDto
            {
                Id = x.Id,
                ReceiptId = x.ReceiptId,
                PurchaseOrderItemId = x.PurchaseOrderItemId,
                ItemDescription = x.PurchaseOrderItem?.Item?.Description ?? string.Empty,
                Unit = x.PurchaseOrderItem?.Unit ?? string.Empty,
                QuantityReceived = x.QuantityReceived,
                Observation = x.Observation,
                HasDivergence = x.HasDivergence,
                DivergenceQuantity = x.DivergenceQuantity
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
