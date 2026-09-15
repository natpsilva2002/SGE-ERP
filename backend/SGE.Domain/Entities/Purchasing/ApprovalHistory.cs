using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class ApprovalHistory : BaseEntity
{
    public Guid ApprovalId { get; private set; }

    public Guid UserId { get; private set; }

    public ApprovalStatus Status { get; private set; }

    public string? Observation { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Approval Approval { get; private set; } = null!;

    public User User { get; private set; } = null!;

    private ApprovalHistory()
    {
    }

    public ApprovalHistory(
        Guid approvalId,
        Guid userId,
        ApprovalStatus status,
        string? observation)
    {
        if (status == ApprovalStatus.Pending)
            throw new ArgumentException(
                "O historico deve registrar uma aprovacao ou rejeicao.");

        ApprovalId = approvalId;
        UserId = userId;
        Status = status;
        Observation = observation;
        CreatedAt = DateTime.UtcNow;
    }
}
