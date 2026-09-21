using SGE.Domain.Enums;

namespace SGE.Application.DTOs.PurchaseRequest;

public class PurchaseRequestDto
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid WorkId { get; set; }

    public Guid RequestedByUserId { get; set; }

    public string? RequestedByUserName { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public PurchaseRequestType Type { get; set; }

    public string? ServiceSpecification { get; set; }

    public decimal? ServiceQuantity { get; set; }

    public string? ServiceUnit { get; set; }

    public Guid? ServiceUnitOfMeasureId { get; set; }

    public PurchaseRequestStatus Status { get; set; }

    public string? WorkflowStatus { get; set; }

    public bool HasQuotation { get; set; }
}
