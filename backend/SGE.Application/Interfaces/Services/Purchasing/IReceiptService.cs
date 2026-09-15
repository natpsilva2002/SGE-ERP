using SGE.Application.DTOs.Receipt;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IReceiptService
{
    Task<IEnumerable<ReceiptDto>> GetAllAsync();

    Task<ReceiptDto?> GetByIdAsync(Guid id);

    Task<IEnumerable<ReceiptDto>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId);

    Task<ReceiptDto?> ReceiveAsync(Guid purchaseOrderId, ReceivePurchaseOrderDto dto);

    Task<ReceiptDto?> AttachInvoiceAsync(
        Guid id,
        string? invoiceNumber,
        string? originalFileName,
        string? storedRelativePath);

    Task<(string FilePath, string FileName)?> GetInvoiceAsync(Guid id);
}
