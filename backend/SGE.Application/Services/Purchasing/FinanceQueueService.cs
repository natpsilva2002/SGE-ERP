using SGE.Application.DTOs.Finance;
using SGE.Application.DTOs.PurchaseOrder;
using SGE.Application.DTOs.ServiceOrder;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Application.Services.Purchasing;

public class FinanceQueueService : IFinanceQueueService
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IServiceOrderService _serviceOrderService;

    public FinanceQueueService(
        IPurchaseOrderService purchaseOrderService,
        IServiceOrderService serviceOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
        _serviceOrderService = serviceOrderService;
    }

    public async Task<IEnumerable<FinanceQueueItemDto>> GetAllAsync()
    {
        // Both services use repositories backed by the same scoped DbContext.
        // Load them sequentially so the queue does not start concurrent EF
        // operations on that context.
        var purchaseOrders = await _purchaseOrderService.GetAllAsync();
        var serviceOrders = await _serviceOrderService.GetAllAsync();

        var materialItems = purchaseOrders
            .Where(IsPurchaseOrderFinanceRelevant)
            .Select(MapPurchaseOrder);

        var serviceItems = serviceOrders
            .Where(IsServiceOrderFinanceRelevant)
            .Select(MapServiceOrder);

        return materialItems
            .Concat(serviceItems)
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.Number);
    }

    private static bool IsPurchaseOrderFinanceRelevant(PurchaseOrderDto order)
    {
        return order.Status == PurchaseOrderStatus.Approved ||
            order.Status == PurchaseOrderStatus.Sent ||
            order.Status == PurchaseOrderStatus.PartiallyReceived ||
            order.Status == PurchaseOrderStatus.Received ||
            order.Status == PurchaseOrderStatus.PartiallyCompleted;
    }

    private static bool IsServiceOrderFinanceRelevant(ServiceOrderDto order)
    {
        // A non-cancelled service order with a positive contract is a valid
        // financial obligation. Waiting-contract orders remain informational;
        // this does not grant or create a payment action.
        return order.ExecutionStatus != ServiceOrderExecutionStatus.Cancelled &&
            order.CurrentContractedValue > 0;
    }

    private static FinanceQueueItemDto MapPurchaseOrder(PurchaseOrderDto order)
    {
        var paymentStatus = order.PaymentStatus switch
        {
            PurchaseOrderPaymentStatus.Paid => FinanceQueuePaymentStatus.Paid,
            PurchaseOrderPaymentStatus.PartiallyPaid => FinanceQueuePaymentStatus.PartiallyPaid,
            _ when order.IsPaymentApproved => FinanceQueuePaymentStatus.Authorized,
            _ => FinanceQueuePaymentStatus.AwaitingApproval
        };

        return new FinanceQueueItemDto
        {
            Id = order.Id,
            DocumentType = FinanceQueueDocumentType.Material,
            Number = order.Number,
            SupplierName = order.SupplierName,
            WorkName = order.WorkName,
            TotalValue = order.TotalValue,
            AmountPaid = order.AmountPaid,
            AmountPending = order.AmountPending,
            PaymentStatus = paymentStatus,
            Date = order.IssueDate
        };
    }

    private static FinanceQueueItemDto MapServiceOrder(ServiceOrderDto order)
    {
        var paymentStatus = order.PaymentStatus switch
        {
            ServiceOrderPaymentStatus.Paid => FinanceQueuePaymentStatus.Paid,
            ServiceOrderPaymentStatus.PartiallyPaid => FinanceQueuePaymentStatus.PartiallyPaid,
            _ => FinanceQueuePaymentStatus.Unpaid
        };

        return new FinanceQueueItemDto
        {
            Id = order.Id,
            DocumentType = FinanceQueueDocumentType.Service,
            Number = order.Number,
            SupplierName = order.SupplierName,
            WorkName = order.WorkName,
            TotalValue = order.CurrentContractedValue,
            AmountPaid = order.AmountPaid,
            AmountPending = order.AmountPending,
            PaymentStatus = paymentStatus,
            Date = order.CreatedAt
        };
    }
}
