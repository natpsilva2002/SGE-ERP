using SGE.Application.DTOs.PurchaseOrder;

namespace SGE.Application.DTOs.Quotation;

public class QuotationApprovalResultDto
{
    public QuotationDto Quotation { get; set; } = null!;

    public IEnumerable<PurchaseOrderDto> PurchaseOrders { get; set; } =
        new List<PurchaseOrderDto>();
}
