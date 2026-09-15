using SGE.Domain.Enums;

namespace SGE.Application.DTOs.ServiceOrder;

public class ServiceOrderPaymentDto
{
    public Guid Id { get; set; }

    public Guid ServiceOrderId { get; set; }

    public Guid PaidByUserId { get; set; }

    public Guid? AdvancePaymentRequestId { get; set; }

    public string? PaidByUserName { get; set; }

    public DateTime PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public string? Observation { get; set; }

    public PaymentStatus Status { get; set; }
}
