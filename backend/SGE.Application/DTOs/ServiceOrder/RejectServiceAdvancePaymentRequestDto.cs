namespace SGE.Application.DTOs.ServiceOrder;

public class RejectServiceAdvancePaymentRequestDto
{
    public Guid RejectedByUserId { get; set; }

    public string RejectionReason { get; set; } = string.Empty;
}
