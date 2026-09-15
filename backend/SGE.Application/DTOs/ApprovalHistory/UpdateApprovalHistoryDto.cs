namespace SGE.Application.DTOs.ApprovalHistory;

public class UpdateApprovalHistoryDto
{
    public bool Approved { get; set; }

    public string? Observation { get; set; }
}