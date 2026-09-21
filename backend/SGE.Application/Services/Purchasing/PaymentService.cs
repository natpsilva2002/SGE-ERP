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
        var payments = await _paymentRepository.GetAllWithDetailsAsync();

        return payments.OrderByDescending(x => x.PaymentDate).Select(MapToDto);
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

        return payments.OrderByDescending(x => x.PaymentDate).Select(MapToDto);
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

    public async Task<PaymentDto?> AddAttachmentAsync(Guid paymentId, string originalFileName, string filePath, string contentType, long fileSizeBytes, Guid uploadedByUserId)
    {
        var payment = await _paymentRepository.GetByIdWithAttachmentsAsync(paymentId);
        if (payment == null) return null;
        var attachment = new PaymentAttachment(paymentId, originalFileName, filePath, contentType, fileSizeBytes, uploadedByUserId);
        payment.Attachments.Add(attachment);
        await _paymentRepository.AddAttachmentAsync(attachment);
        await _paymentRepository.SaveChangesAsync();
        return MapToDto(payment);
    }

    public async Task<(string FilePath, string FileName, string ContentType)?> GetAttachmentAsync(Guid paymentId, Guid attachmentId)
    {
        var payment = await _paymentRepository.GetByIdWithAttachmentsAsync(paymentId);
        var attachment = payment?.Attachments.FirstOrDefault(x => x.Id == attachmentId);
        return attachment == null ? null : (attachment.FilePath, attachment.OriginalFileName, attachment.ContentType);
    }

    public async Task<(bool Deleted, string? FilePath)> DeleteAttachmentAsync(Guid paymentId, Guid attachmentId)
    {
        var payment = await _paymentRepository.GetByIdWithAttachmentsAsync(paymentId);
        var attachment = payment?.Attachments.FirstOrDefault(x => x.Id == attachmentId);
        if (attachment == null) return (false, null);
        var path = attachment.FilePath;
        payment!.Attachments.Remove(attachment);
        _paymentRepository.RemoveAttachment(attachment);
        await _paymentRepository.SaveChangesAsync();
        return (true, path);
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            PurchaseOrderId = payment.PurchaseOrderId,
            PaidByUserId = payment.PaidByUserId,
            PaidByUserName = FormatUserName(payment.PaidByUser),
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
            ,Attachments = payment.Attachments.OrderBy(x => x.UploadedAt).Select(x => new PaymentAttachmentDto
            {
                Id = x.Id, PaymentId = x.PaymentId, OriginalFileName = x.OriginalFileName,
                ContentType = x.ContentType, FileSizeBytes = x.FileSizeBytes,
                UploadedByUserId = x.UploadedByUserId, UploadedAt = x.UploadedAt
            }).ToList()
        };
    }

    private static string? FormatUserName(SGE.Domain.Entities.Administration.User? user)
    {
        if (user == null)
            return null;

        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Email : name;
    }

    private static DateTime NormalizePaymentDate(DateTime? paymentDate)
    {
        if (!paymentDate.HasValue)
            return DateTime.UtcNow;

        return paymentDate.Value.Kind switch
        {
            DateTimeKind.Utc => paymentDate.Value,
            DateTimeKind.Local => paymentDate.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(paymentDate.Value, DateTimeKind.Local).ToUniversalTime()
        };
    }
}
