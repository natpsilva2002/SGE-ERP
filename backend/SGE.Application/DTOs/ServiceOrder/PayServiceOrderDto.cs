using SGE.Domain.Enums;

namespace SGE.Application.DTOs.ServiceOrder;

public class PayServiceOrderDto
{
    public Guid PaidByUserId { get; set; }

    public Guid? AdvancePaymentRequestId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public string? Observation { get; set; }
}
