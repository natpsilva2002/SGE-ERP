using SGE.Application.DTOs.Payment;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Application.Services.Purchasing;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUserRepository _userRepository;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IUserRepository userRepository)
    {
        _paymentRepository = paymentRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<PaymentDto>> GetAllAsync()
    {
        var payments = await _paymentRepository.GetAllAsync();

        return payments.Select(MapToDto);
    }

    public async Task<PaymentDto?> GetByIdAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);

        if (payment == null)
            return null;

        return MapToDto(payment);
    }

    public async Task<IEnumerable<PaymentDto>> GetByPurchaseOrderIdAsync(
        Guid purchaseOrderId)
    {
        var payments = await _paymentRepository
            .GetByPurchaseOrderIdAsync(purchaseOrderId);

        return payments.Select(MapToDto);
    }

    public async Task<PaymentDto?> PayAsync(
        Guid purchaseOrderId,
        PayPurchaseOrderDto dto)
    {
        var purchaseOrder = await _purchaseOrderRepository
            .GetWithItemsByIdAsync(purchaseOrderId);

        if (purchaseOrder == null)
            return null;

        var user = await _userRepository.GetByIdAsync(dto.PaidByUserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        purchaseOrder.RegisterPayment(dto.Amount);

        var payment = new Payment(
            purchaseOrder.Id,
            dto.PaidByUserId,
            dto.Amount,
            NormalizePaymentDate(dto.PaymentDate),
            dto.PaymentMethod,
            dto.TransactionReference,
            dto.DueDate,
            dto.InvoiceNumber,
            dto.InvoiceFileName,
            dto.InvoiceFilePath,
            dto.Observation);

        await _paymentRepository.AddAsync(payment);
        _purchaseOrderRepository.Update(purchaseOrder);
        await _paymentRepository.SaveChangesAsync();

        return MapToDto(payment);
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            PurchaseOrderId = payment.PurchaseOrderId,
            PaidByUserId = payment.PaidByUserId,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            TransactionReference = payment.TransactionReference,
            DueDate = payment.DueDate,
            InvoiceNumber = payment.InvoiceNumber,
            InvoiceFileName = payment.InvoiceFileName,
            InvoiceFilePath = payment.InvoiceFilePath,
            Observation = payment.Observation,
            Status = payment.Status
        };
    }

    private static DateTime NormalizePaymentDate(DateTime? paymentDate)
    {
        if (!paymentDate.HasValue)
            return DateTime.UtcNow;

        return paymentDate.Value.Kind switch
        {
            DateTimeKind.Utc => paymentDate.Value,
            DateTimeKind.Local => paymentDate.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(paymentDate.Value, DateTimeKind.Utc)
        };
    }
}
