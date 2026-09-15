using SGE.Domain.Enums;

namespace SGE.Application.DTOs.Approval;

public class UpdateApprovalDto
{
    public ApprovalStatus Status { get; set; }

    public string? Observation { get; set; }
}
