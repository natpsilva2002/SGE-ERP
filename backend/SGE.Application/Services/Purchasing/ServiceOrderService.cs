using SGE.Application.DTOs.ServiceOrder;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Repositories.Catalog;
using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Application.Services.Purchasing;

public class ServiceOrderService : IServiceOrderService
{
    private readonly IServiceOrderRepository _repository;
    private readonly IServiceOrderPaymentRepository _paymentRepository;
    private readonly IServiceAdvancePaymentRequestRepository _advancePaymentRequestRepository;
    private readonly IServiceOrderAttachmentRepository _attachmentRepository;
    private readonly IPurchaseRequestRepository _purchaseRequestRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfMeasureRepository _unitRepository;

    public ServiceOrderService(
        IServiceOrderRepository repository,
        IServiceOrderPaymentRepository paymentRepository,
        IServiceAdvancePaymentRequestRepository advancePaymentRequestRepository,
        IServiceOrderAttachmentRepository attachmentRepository,
        IPurchaseRequestRepository purchaseRequestRepository,
        ISupplierRepository supplierRepository,
        IUserRepository userRepository,
        IUnitOfMeasureRepository unitRepository)
    {
        _repository = repository;
        _paymentRepository = paymentRepository;
        _advancePaymentRequestRepository = advancePaymentRequestRepository;
        _attachmentRepository = attachmentRepository;
        _purchaseRequestRepository = purchaseRequestRepository;
        _supplierRepository = supplierRepository;
        _userRepository = userRepository;
        _unitRepository = unitRepository;
    }

    public async Task<IEnumerable<ServiceOrderDto>> GetAllAsync()
    {
        var serviceOrders = await _repository.GetAllWithDetailsAsync();

        return serviceOrders.OrderByDescending(x => x.CreatedAt).Select(MapToDto);
    }

    public async Task<ServiceOrderDto?> GetByIdAsync(Guid id)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        if (serviceOrder == null)
            return null;

        return MapToDto(serviceOrder);
    }

    public async Task<ServiceOrderDto> CreateAsync(CreateServiceOrderDto dto)
    {
        var purchaseRequest = await _purchaseRequestRepository.GetByIdAsync(dto.PurchaseRequestId);

        if (purchaseRequest == null)
            throw new ArgumentException("A solicitacao de compra informada nao existe.");

        if (purchaseRequest.Type != PurchaseRequestType.Service)
            throw new InvalidOperationException("Apenas solicitacoes de servico podem gerar ordem de servico.");

        if (purchaseRequest.Status != PurchaseRequestStatus.Approved)
            throw new InvalidOperationException("A solicitacao de servico precisa estar aprovada para gerar ordem de servico.");

        if (await _repository.ExistsForPurchaseRequestAsync(purchaseRequest.Id))
            throw new InvalidOperationException("Ja existe uma ordem de servico para esta solicitacao.");

        var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId);

        if (supplier == null)
            throw new ArgumentException("O prestador informado nao existe.");

        if (dto.ContractedQuantity <= 0 || string.IsNullOrWhiteSpace(dto.Unit))
            throw new ArgumentException("A quantidade contratada e a unidade de medicao sao obrigatorias.");
        var units = await _unitRepository.FindAsync(x => x.Code == dto.Unit && !x.IsDeleted);
        if (!units.Any())
            throw new ArgumentException("Selecione uma unidade de medicao ativa.");

        var serviceOrder = new ServiceOrder(
            purchaseRequest.Id,
            purchaseRequest.WorkId,
            dto.SupplierId,
            await GenerateNumberAsync(),
            purchaseRequest.Description,
            purchaseRequest.ServiceSpecification,
            dto.ContractedQuantity,
            dto.Unit.Trim(),
            dto.ContractedValue,
            dto.PaymentCondition,
            dto.InstallmentCount);

        await _repository.AddAsync(serviceOrder);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(serviceOrder.Id) ?? MapToDto(serviceOrder);
    }

    public async Task<ServiceOrderDto?> UpdateContractTermsAsync(Guid id, UpdateServiceOrderContractDto dto)
    {
        var order = await _repository.GetWithDetailsByIdAsync(id);
        if (order == null) return null;
        var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId);
        if (supplier == null) throw new ArgumentException("O prestador informado nao existe.");
        var units = await _unitRepository.FindAsync(x => x.Code == dto.Unit && !x.IsDeleted);
        if (!units.Any()) throw new ArgumentException("Selecione uma unidade de medicao ativa.");
        order.UpdateContractTerms(dto.SupplierId, dto.ContractedValue, dto.ContractedQuantity,
            dto.Unit, dto.PaymentCondition, dto.InstallmentCount);
        _repository.Update(order);
        await _repository.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<ServiceOrderDto?> AttachContractAsync(
        Guid id,
        string originalFileName,
        string storedRelativePath,
        Guid uploadedByUserId)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        if (serviceOrder == null)
            return null;

        var user = await _userRepository.GetByIdAsync(uploadedByUserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        serviceOrder.AttachContract(
            originalFileName,
            storedRelativePath,
            uploadedByUserId);

        _repository.Update(serviceOrder);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<ServiceOrderDto?> ReleaseAsync(Guid id)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        if (serviceOrder == null)
            return null;

        serviceOrder.Release();

        _repository.Update(serviceOrder);
        await _repository.SaveChangesAsync();

        return MapToDto(serviceOrder);
    }

    public async Task<ServiceOrderDto?> PayAsync(Guid id, PayServiceOrderDto dto)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        if (serviceOrder == null)
            return null;

        serviceOrder.EnsureReleasedForFinancialOperation();

        var user = await _userRepository.GetByIdAsync(dto.PaidByUserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        if (dto.Amount <= 0)
            throw new ArgumentException("O valor do pagamento deve ser maior que zero.");

        Guid? advancePaymentRequestId = null;

        if (dto.AdvancePaymentRequestId.HasValue)
        {
            var advanceRequest = serviceOrder.AdvancePaymentRequests
                .FirstOrDefault(x => x.Id == dto.AdvancePaymentRequestId.Value);

            if (advanceRequest == null)
                throw new ArgumentException("A solicitacao de antecipacao informada nao existe.");

            if (advanceRequest.Status != ServiceAdvancePaymentStatus.Approved)
                throw new InvalidOperationException("A antecipacao precisa estar aprovada para pagamento.");

            var advancePending = GetAdvanceRequestPendingAmount(advanceRequest);

            if (dto.Amount > advancePending)
                throw new InvalidOperationException("O pagamento nao pode ultrapassar o saldo da antecipacao aprovada.");

            advancePaymentRequestId = advanceRequest.Id;
        }
        else
        {
            var availableMeasuredToPay = GetAvailableMeasuredToPay(serviceOrder);

            if (dto.Amount > availableMeasuredToPay)
                throw new InvalidOperationException(
                    "O pagamento nao pode ultrapassar o valor medido e aprovado disponivel.");
        }

        var payment = new ServiceOrderPayment(
            serviceOrder.Id,
            dto.PaidByUserId,
            dto.Amount,
            NormalizePaymentDate(dto.PaymentDate),
            dto.PaymentMethod,
            dto.Observation,
            advancePaymentRequestId);

        serviceOrder.RegisterPayment(dto.Amount);

        await _paymentRepository.AddAsync(payment);
        await _paymentRepository.SaveChangesAsync();

        var result = await GetByIdAsync(id);
        if (result != null)
            result.LastPaymentId = payment.Id;

        return result;
    }

    public async Task<ServiceOrderDto?> AddPaymentAttachmentAsync(
        Guid serviceOrderId,
        Guid paymentId,
        string originalFileName,
        string storedRelativePath,
        string contentType,
        long fileSizeBytes,
        Guid uploadedByUserId)
    {
        var payment = await _paymentRepository.GetByServiceOrderAndIdWithAttachmentsAsync(serviceOrderId, paymentId);
        if (payment == null)
            return null;

        var attachment = new ServiceOrderPaymentAttachment(
            paymentId, originalFileName, storedRelativePath, contentType, fileSizeBytes, uploadedByUserId);
        payment.Attachments.Add(attachment);
        await _paymentRepository.AddAttachmentAsync(attachment);
        await _paymentRepository.SaveChangesAsync();

        return await GetByIdAsync(serviceOrderId);
    }

    public async Task<(string FilePath, string FileName, string ContentType)?> GetPaymentAttachmentAsync(
        Guid serviceOrderId, Guid paymentId, Guid attachmentId)
    {
        var payment = await _paymentRepository.GetByServiceOrderAndIdWithAttachmentsAsync(serviceOrderId, paymentId);
        var attachment = payment?.Attachments.FirstOrDefault(x => x.Id == attachmentId);
        return attachment == null
            ? null
            : (attachment.FilePath, attachment.OriginalFileName, attachment.ContentType);
    }

    public async Task<ServiceOrderDto?> RequestAdvancePaymentAsync(
        Guid id,
        CreateServiceAdvancePaymentRequestDto dto)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        if (serviceOrder == null)
            return null;

        serviceOrder.EnsureReleasedForFinancialOperation();

        var user = await _userRepository.GetByIdAsync(dto.RequestedByUserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        if (dto.Amount <= 0)
            throw new ArgumentException("O valor da antecipacao deve ser maior que zero.");

        var committedAdvance = serviceOrder.AdvancePaymentRequests
            .Where(x => x.Status != ServiceAdvancePaymentStatus.Rejected)
            .Sum(x => Math.Max(x.Amount - x.Payments.Sum(p => p.Amount), 0));

        if (serviceOrder.AmountPaid + committedAdvance + dto.Amount > serviceOrder.CurrentContractedValue)
            throw new InvalidOperationException("A antecipacao nao pode ultrapassar o saldo contratado da ordem de servico.");

        var request = new ServiceAdvancePaymentRequest(
            serviceOrder.Id,
            dto.RequestedByUserId,
            dto.Amount,
            dto.Observation);

        await _advancePaymentRequestRepository.AddAsync(request);
        await _advancePaymentRequestRepository.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<ServiceOrderDto?> ApproveAdvancePaymentAsync(
        Guid id,
        Guid advancePaymentRequestId,
        Guid approvedByUserId)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        if (serviceOrder == null)
            return null;

        serviceOrder.EnsureReleasedForFinancialOperation();

        var user = await _userRepository.GetByIdAsync(approvedByUserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        var request = serviceOrder.AdvancePaymentRequests
            .FirstOrDefault(x => x.Id == advancePaymentRequestId);

        if (request == null)
            throw new ArgumentException("A solicitacao de antecipacao informada nao existe.");

        request.Approve(approvedByUserId);

        _advancePaymentRequestRepository.Update(request);
        await _advancePaymentRequestRepository.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<ServiceOrderDto?> RejectAdvancePaymentAsync(
        Guid id,
        Guid advancePaymentRequestId,
        RejectServiceAdvancePaymentRequestDto dto)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        if (serviceOrder == null)
            return null;

        var user = await _userRepository.GetByIdAsync(dto.RejectedByUserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        var request = serviceOrder.AdvancePaymentRequests
            .FirstOrDefault(x => x.Id == advancePaymentRequestId);

        if (request == null)
            throw new ArgumentException("A solicitacao de antecipacao informada nao existe.");

        request.Reject(dto.RejectedByUserId, dto.RejectionReason);

        _advancePaymentRequestRepository.Update(request);
        await _advancePaymentRequestRepository.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<ServiceOrderDto?> AddAttachmentAsync(
        Guid id,
        ServiceOrderAttachmentType type,
        string originalFileName,
        string storedRelativePath,
        string contentType,
        long fileSizeBytes,
        Guid uploadedByUserId)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        if (serviceOrder == null)
            return null;

        var user = await _userRepository.GetByIdAsync(uploadedByUserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        var attachment = new ServiceOrderAttachment(
            id,
            type,
            originalFileName,
            storedRelativePath,
            contentType,
            fileSizeBytes,
            uploadedByUserId);

        await _attachmentRepository.AddAsync(attachment);
        await _attachmentRepository.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<(string FilePath, string FileName, string ContentType)?> GetAttachmentAsync(
        Guid id,
        Guid attachmentId)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        var attachment = serviceOrder?.Attachments.FirstOrDefault(x => x.Id == attachmentId);

        if (attachment == null)
            return null;

        return (attachment.FilePath, attachment.OriginalFileName, attachment.ContentType);
    }

    public async Task<(bool Deleted, string? FilePath)> DeleteAttachmentAsync(Guid id, Guid attachmentId)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        var attachment = serviceOrder?.Attachments.FirstOrDefault(x => x.Id == attachmentId);

        if (attachment == null)
            return (false, null);

        var filePath = attachment.FilePath;

        _attachmentRepository.Remove(attachment);
        await _attachmentRepository.SaveChangesAsync();

        return (true, filePath);
    }

    public async Task<(string FilePath, string FileName)?> GetContractAsync(Guid id)
    {
        var serviceOrder = await _repository.GetWithDetailsByIdAsync(id);

        if (serviceOrder == null ||
            string.IsNullOrWhiteSpace(serviceOrder.ContractFilePath) ||
            string.IsNullOrWhiteSpace(serviceOrder.ContractFileName))
            return null;

        return (serviceOrder.ContractFilePath, serviceOrder.ContractFileName);
    }

    private async Task<string> GenerateNumberAsync()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
            var number = $"OS-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{suffix}";

            if (!await _repository.ExistsByNumberAsync(number))
                return number;
        }

        throw new InvalidOperationException("Nao foi possivel gerar um numero unico para a ordem de servico.");
    }

    private static ServiceOrderDto MapToDto(ServiceOrder serviceOrder)
    {
        return new ServiceOrderDto
        {
            Id = serviceOrder.Id,
            CreatedAt = serviceOrder.CreatedAt,
            Number = serviceOrder.Number,
            PurchaseRequestId = serviceOrder.PurchaseRequestId,
            PurchaseRequestNumber = serviceOrder.PurchaseRequest?.Number ?? string.Empty,
            RequestedByUserName = FormatUserName(serviceOrder.PurchaseRequest?.RequestedByUser),
            WorkId = serviceOrder.WorkId,
            WorkName = serviceOrder.Work?.Name ?? string.Empty,
            SupplierId = serviceOrder.SupplierId,
            SupplierName = serviceOrder.Supplier?.TradeName ?? serviceOrder.Supplier?.CorporateName ?? string.Empty,
            SupplierDocument = serviceOrder.Supplier?.Document ?? string.Empty,
            SupplierEmail = serviceOrder.Supplier?.Email ?? string.Empty,
            SupplierPhone = serviceOrder.Supplier?.Phone ?? string.Empty,
            ServiceDescription = serviceOrder.ServiceDescription,
            ServiceSpecification = serviceOrder.ServiceSpecification,
            EstimatedQuantity = serviceOrder.EstimatedQuantity,
            Unit = serviceOrder.Unit,
            ContractedValue = serviceOrder.ContractedValue,
            CurrentContractedValue = serviceOrder.CurrentContractedValue,
            CurrentContractedQuantity = serviceOrder.CurrentContractedQuantity,
            IsReleasedForExecution = serviceOrder.ExecutionStatus is ServiceOrderExecutionStatus.Released or
                ServiceOrderExecutionStatus.InProgress or ServiceOrderExecutionStatus.Completed,
            PaymentCondition = serviceOrder.PaymentCondition,
            InstallmentCount = serviceOrder.InstallmentCount,
            ExecutionStatus = serviceOrder.ExecutionStatus,
            PaymentStatus = serviceOrder.PaymentStatus,
            AmountPaid = serviceOrder.AmountPaid,
            AmountPending = serviceOrder.AmountPending,
            AvailableToPay = GetAvailableToPay(serviceOrder),
            AvailableMeasuredToPay = GetAvailableMeasuredToPay(serviceOrder),
            AvailableAdvanceToPay = GetAvailableAdvanceToPay(serviceOrder),
            UnmeasuredBalance = Math.Max(serviceOrder.CurrentContractedValue - GetApprovedMeasuredAmount(serviceOrder), 0),
            ContractFileName = serviceOrder.ContractFileName,
            ContractUploadedAt = serviceOrder.ContractUploadedAt,
            ContractUploadedByUserId = serviceOrder.ContractUploadedByUserId,
            ContractUploadedByUserName = serviceOrder.ContractUploadedByUser == null
                ? null
                : $"{serviceOrder.ContractUploadedByUser.FirstName} {serviceOrder.ContractUploadedByUser.LastName}".Trim(),
            ApprovedMeasuredAmount = GetApprovedMeasuredAmount(serviceOrder),
            CommittedMeasuredAmount = GetCommittedMeasuredAmount(serviceOrder),
            RemainingToMeasure = Math.Max(serviceOrder.CurrentContractedValue - GetCommittedMeasuredAmount(serviceOrder), 0),
            ExecutionPercentage = serviceOrder.CurrentContractedValue <= 0
                ? 0
                : Math.Round(GetApprovedMeasuredAmount(serviceOrder) / serviceOrder.CurrentContractedValue * 100, 2),
            MeasuredQuantity = GetMeasuredQuantity(serviceOrder),
            RemainingQuantity = serviceOrder.CurrentContractedQuantity.HasValue
                ? Math.Max(serviceOrder.CurrentContractedQuantity.Value - GetMeasuredQuantity(serviceOrder), 0)
                : 0,
            PhysicalPercentage = serviceOrder.CurrentContractedQuantity is > 0
                ? Math.Round(GetMeasuredQuantity(serviceOrder) / serviceOrder.CurrentContractedQuantity.Value * 100, 2)
                : 0,
            FinancialPercentage = serviceOrder.CurrentContractedValue <= 0
                ? 0
                : Math.Round(GetApprovedMeasuredAmount(serviceOrder) / serviceOrder.CurrentContractedValue * 100, 2),
            Measurements = serviceOrder.Measurements
                .OrderBy(x => x.MeasurementNumber)
                .ThenBy(x => x.CreatedAt)
                .Select(ServiceMeasurementService.MapToDto),
            Payments = serviceOrder.Payments
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.Id)
                .Select(MapPaymentToDto),
            AdvancePaymentRequests = serviceOrder.AdvancePaymentRequests
                .OrderByDescending(x => x.RequestedAt)
                .ThenByDescending(x => x.Id)
                .Select(MapAdvancePaymentRequestToDto),
            Attachments = serviceOrder.Attachments
                .OrderByDescending(x => x.UploadedAt)
                .ThenByDescending(x => x.Id)
                .Select(MapAttachmentToDto),
            Amendments = serviceOrder.Amendments
                .OrderBy(x => x.CreatedAt)
                .Select(x => ServiceOrderAmendmentService.Map(x, x.Approvals.OrderByDescending(a => a.CreatedAt).FirstOrDefault()))
        };
    }

    private static ServiceOrderPaymentDto MapPaymentToDto(ServiceOrderPayment payment)
    {
        return new ServiceOrderPaymentDto
        {
            Id = payment.Id,
            ServiceOrderId = payment.ServiceOrderId,
            PaidByUserId = payment.PaidByUserId,
            AdvancePaymentRequestId = payment.AdvancePaymentRequestId,
            PaidByUserName = FormatUserName(payment.PaidByUser),
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Observation = payment.Observation,
            Status = payment.Status,
            Attachments = payment.Attachments.OrderBy(x => x.UploadedAt).Select(x => new ServiceOrderPaymentAttachmentDto
            {
                Id = x.Id,
                ServiceOrderPaymentId = x.Id,
                OriginalFileName = x.OriginalFileName,
                ContentType = x.ContentType,
                FileSizeBytes = x.FileSizeBytes,
                UploadedByUserId = x.UploadedByUserId,
                UploadedAt = x.UploadedAt
            }).ToList()
        };
    }

    private static ServiceAdvancePaymentRequestDto MapAdvancePaymentRequestToDto(
        ServiceAdvancePaymentRequest request)
    {
        var amountPaid = request.Payments.Sum(x => x.Amount);

        return new ServiceAdvancePaymentRequestDto
        {
            Id = request.Id,
            ServiceOrderId = request.ServiceOrderId,
            RequestedByUserId = request.RequestedByUserId,
            RequestedByUserName = FormatUserName(request.RequestedByUser),
            RequestedAt = request.RequestedAt,
            Amount = request.Amount,
            AmountPaid = amountPaid,
            AmountPending = Math.Max(request.Amount - amountPaid, 0),
            Observation = request.Observation,
            Status = request.Status,
            ApprovedByUserId = request.ApprovedByUserId,
            ApprovedByUserName = FormatUserName(request.ApprovedByUser),
            ApprovedAt = request.ApprovedAt,
            RejectedByUserId = request.RejectedByUserId,
            RejectedByUserName = FormatUserName(request.RejectedByUser),
            RejectedAt = request.RejectedAt,
            RejectionReason = request.RejectionReason
        };
    }

    private static ServiceOrderAttachmentDto MapAttachmentToDto(ServiceOrderAttachment attachment)
    {
        return new ServiceOrderAttachmentDto
        {
            Id = attachment.Id,
            ServiceOrderId = attachment.ServiceOrderId,
            Type = attachment.Type,
            OriginalFileName = attachment.OriginalFileName,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            UploadedByUserId = attachment.UploadedByUserId,
            UploadedByUserName = FormatUserName(attachment.UploadedByUser),
            UploadedAt = attachment.UploadedAt
        };
    }

    private static string? FormatUserName(SGE.Domain.Entities.Administration.User? user)
    {
        if (user == null)
            return null;

        var name = $"{user.FirstName} {user.LastName}".Trim();

        return string.IsNullOrWhiteSpace(name) ? user.Email : name;
    }

    private static decimal GetApprovedMeasuredAmount(ServiceOrder serviceOrder)
    {
        return serviceOrder.Measurements
            .Where(x => x.Status == ServiceMeasurementStatus.Approved)
            .Sum(x => x.Amount);
    }

    private static decimal GetMeasuredQuantity(ServiceOrder serviceOrder)
    {
        return serviceOrder.Measurements
            .Where(x => x.Status == ServiceMeasurementStatus.Approved)
            .Sum(x => x.QuantityMeasured);
    }

    private static decimal GetCommittedMeasuredAmount(ServiceOrder serviceOrder)
    {
        return serviceOrder.Measurements
            .Where(x => x.Status != ServiceMeasurementStatus.Rejected)
            .Sum(x => x.Amount);
    }

    private static decimal GetAvailableToPay(ServiceOrder serviceOrder)
    {
        return GetAvailableMeasuredToPay(serviceOrder) + GetAvailableAdvanceToPay(serviceOrder);
    }

    private static decimal GetAvailableMeasuredToPay(ServiceOrder serviceOrder)
    {
        return Math.Max(GetApprovedMeasuredAmount(serviceOrder) - serviceOrder.AmountPaid, 0);
    }

    private static decimal GetAvailableAdvanceToPay(ServiceOrder serviceOrder)
    {
        return serviceOrder.AdvancePaymentRequests
            .Where(x => x.Status == ServiceAdvancePaymentStatus.Approved)
            .Sum(GetAdvanceRequestPendingAmount);
    }

    private static decimal GetAdvanceRequestPendingAmount(ServiceAdvancePaymentRequest request)
    {
        return Math.Max(request.Amount - request.Payments.Sum(x => x.Amount), 0);
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
