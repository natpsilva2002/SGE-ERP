namespace SGE.Application.DTOs.PurchaseRequest;

public class UpdatePurchaseRequestDto
{
    public string Number { get; set; } = string.Empty;

    public Guid? WorkId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? ServiceSpecification { get; set; }

    public decimal? ServiceQuantity { get; set; }

    public string? ServiceUnit { get; set; }
}
