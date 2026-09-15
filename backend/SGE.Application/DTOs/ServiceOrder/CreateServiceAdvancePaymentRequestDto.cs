namespace SGE.Application.DTOs.ServiceOrder;

public class CreateServiceAdvancePaymentRequestDto
{
    public Guid RequestedByUserId { get; set; }

    public decimal Amount { get; set; }

    public string? Observation { get; set; }
}
