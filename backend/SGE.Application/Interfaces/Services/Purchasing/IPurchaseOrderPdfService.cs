using SGE.Application.DTOs.PurchaseOrder;

namespace SGE.Application.Interfaces.Services.Purchasing;

public interface IPurchaseOrderPdfService
{
    byte[] Generate(PurchaseOrderDto purchaseOrder);
}
