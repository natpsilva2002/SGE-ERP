using SGE.Domain.Enums;

namespace SGE.Application.DTOs.ApprovalHistory;

public class CreateApprovalHistoryDto
{
    public Guid ApprovalId { get; set; }

    public Guid UserId { get; set; }

    public ApprovalStatus Status { get; set; }

    public string? Observation { get; set; }
}
