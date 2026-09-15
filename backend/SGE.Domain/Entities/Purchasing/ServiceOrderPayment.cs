using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class ServiceOrderPayment : BaseEntity
{
    public Guid ServiceOrderId { get; private set; }

    public Guid PaidByUserId { get; private set; }

    public Guid? AdvancePaymentRequestId { get; private set; }

    public DateTime PaymentDate { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public string? Observation { get; private set; }

    public PaymentStatus Status { get; private set; }

    public ServiceOrder ServiceOrder { get; private set; } = null!;

    public User PaidByUser { get; private set; } = null!;

    public ServiceAdvancePaymentRequest? AdvancePaymentRequest { get; private set; }

    private ServiceOrderPayment()
    {
    }

    public ServiceOrderPayment(
        Guid serviceOrderId,
        Guid paidByUserId,
        decimal amount,
        DateTime paymentDate,
        PaymentMethod paymentMethod,
        string? observation,
        Guid? advancePaymentRequestId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("O valor do pagamento deve ser maior que zero.");

        if (!Enum.IsDefined(paymentMethod))
            throw new ArgumentException("O metodo de pagamento informado e invalido.");

        ServiceOrderId = serviceOrderId;
        PaidByUserId = paidByUserId;
        AdvancePaymentRequestId = advancePaymentRequestId;
        Amount = amount;
        PaymentDate = paymentDate;
        PaymentMethod = paymentMethod;
        Observation = observation;
        Status = PaymentStatus.Registered;
    }
}
