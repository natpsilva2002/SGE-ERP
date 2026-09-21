using SGE.Domain.Enums;

namespace SGE.Application.DTOs.PurchaseRequest;

public class CreatePurchaseRequestDto
{
    public Guid WorkId { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public PurchaseRequestType Type { get; set; } = PurchaseRequestType.Material;

    public string? ServiceSpecification { get; set; }

    public decimal? ServiceQuantity { get; set; }

    public string? ServiceUnit { get; set; }

    public Guid? ServiceUnitOfMeasureId { get; set; }

    public IEnumerable<CreatePurchaseRequestMaterialItemDto> Items { get; set; } =
        Enumerable.Empty<CreatePurchaseRequestMaterialItemDto>();
}

public class CreatePurchaseRequestMaterialItemDto
{
    public Guid ItemId { get; set; }

    public decimal Quantity { get; set; }

    public string? Observation { get; set; }
}
