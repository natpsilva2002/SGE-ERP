namespace SGE.Application.DTOs.Receipt;

public class ReceivePurchaseOrderDto
{
    public Guid ReceivedByUserId { get; set; }

    public string? Observation { get; set; }

    public IEnumerable<ReceivePurchaseOrderItemDto> Items { get; set; } =
        new List<ReceivePurchaseOrderItemDto>();
}
