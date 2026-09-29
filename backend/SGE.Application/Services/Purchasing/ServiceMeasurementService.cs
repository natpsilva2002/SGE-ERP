using SGE.Application.DTOs.ServiceOrder;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Application.Services.Purchasing;

public class ServiceMeasurementService : IServiceMeasurementService
{
    private readonly IServiceMeasurementRepository _repository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUserRepository _userRepository;

    public ServiceMeasurementService(
        IServiceMeasurementRepository repository,
        IServiceOrderRepository serviceOrderRepository,
        IUserRepository userRepository)
    {
        _repository = repository;
        _serviceOrderRepository = serviceOrderRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<ServiceMeasurementDto>> GetByServiceOrderIdAsync(
        Guid serviceOrderId)
    {
        var measurements = await _repository.GetByServiceOrderIdAsync(serviceOrderId);

        return measurements.Select(MapToDto);
    }

    public async Task<ServiceMeasurementDto?> GetByIdAsync(
        Guid serviceOrderId,
        Guid measurementId)
    {
        var measurement = await _repository.GetByServiceOrderAndIdAsync(
            serviceOrderId,
            measurementId);

        return measurement == null ? null : MapToDto(measurement);
    }

    public async Task<ServiceMeasurementDto?> AddAttachmentAsync(
        Guid serviceOrderId,
        Guid measurementId,
        string originalFileName,
        string storedRelativePath,
        string contentType,
        long fileSizeBytes,
        Guid uploadedByUserId)
    {
        var measurement = await _repository.GetByServiceOrderAndIdAsync(serviceOrderId, measurementId);
        if (measurement == null)
            return null;

        await EnsureUserExistsAsync(uploadedByUserId);
        var attachment = new ServiceMeasurementAttachment(
            measurementId, originalFileName, storedRelativePath, contentType, fileSizeBytes, uploadedByUserId);

        measurement.Attachments.Add(attachment);
        await _repository.AddAttachmentAsync(attachment);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(serviceOrderId, measurementId);
    }

    public async Task<(string FilePath, string FileName, string ContentType)?> GetAttachmentAsync(
        Guid serviceOrderId, Guid measurementId, Guid attachmentId)
    {
        var measurement = await _repository.GetByServiceOrderAndIdAsync(serviceOrderId, measurementId);
        var attachment = measurement?.Attachments.FirstOrDefault(x => x.Id == attachmentId);
        return attachment == null
            ? null
            : (attachment.FilePath, attachment.OriginalFileName, attachment.ContentType);
    }

    public async Task<ServiceMeasurementDto> CreateAsync(
        Guid serviceOrderId,
        CreateServiceMeasurementDto dto)
    {
        var serviceOrder = await GetServiceOrderOrThrowAsync(serviceOrderId);

        EnsureCanCreateMeasurement(serviceOrder);
        await EnsureUserExistsAsync(dto.CreatedByUserId);
        await EnsureMeasurementFitsAsync(serviceOrder, dto.QuantityMeasured, dto.Amount);

        var measurement = new ServiceMeasurement(
            serviceOrder.Id,
            await GenerateMeasurementNumberAsync(serviceOrder.Id),
            dto.MeasurementDate,
            dto.Description,
            dto.QuantityMeasured,
            dto.Unit ?? serviceOrder.Unit,
            dto.Amount,
            dto.CreatedByUserId,
            dto.Observation);

        serviceOrder.StartExecution();
        var measuredAmount = await _repository.SumByServiceOrderAsync(serviceOrder.Id, ServiceMeasurementStatus.Approved);
        serviceOrder.RefreshExecutionByApprovedAmount(measuredAmount + dto.Amount);

        await _repository.AddAsync(measurement);
        _serviceOrderRepository.Update(serviceOrder);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(serviceOrder.Id, measurement.Id) ?? MapToDto(measurement);
    }

    public async Task<ServiceMeasurementDto?> UpdateAsync(
        Guid serviceOrderId,
        Guid measurementId,
        UpdateServiceMeasurementDto dto)
    {
        var serviceOrder = await GetServiceOrderOrThrowAsync(serviceOrderId);
        var measurement = await _repository.GetByServiceOrderAndIdAsync(
            serviceOrderId,
            measurementId);

        if (measurement == null)
            return null;

        await EnsureMeasurementFitsAsync(
            serviceOrder,
            dto.QuantityMeasured,
            dto.Amount,
            measurement.Id);

        measurement.Update(
            dto.MeasurementDate,
            dto.Description,
            dto.QuantityMeasured,
            dto.Unit ?? serviceOrder.Unit,
            dto.Amount,
            dto.Observation);

        _repository.Update(measurement);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(serviceOrderId, measurementId);
    }

    public async Task<bool> DeleteAsync(Guid serviceOrderId, Guid measurementId)
    {
        var measurement = await _repository.GetByServiceOrderAndIdAsync(
            serviceOrderId,
            measurementId);

        if (measurement == null)
            return false;

        if (measurement.Status != ServiceMeasurementStatus.Draft)
            throw new InvalidOperationException(
                "Apenas medicoes em rascunho podem ser excluidas.");

        _repository.Remove(measurement);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<ServiceMeasurementDto?> SubmitAsync(
        Guid serviceOrderId,
        Guid measurementId)
    {
        var serviceOrder = await GetServiceOrderOrThrowAsync(serviceOrderId);
        var measurement = await _repository.GetByServiceOrderAndIdAsync(
            serviceOrderId,
            measurementId);

        if (measurement == null)
            return null;

        await EnsureMeasurementFitsAsync(
            serviceOrder,
            measurement.QuantityMeasured,
            measurement.Amount,
            measurement.Id);

        measurement.Submit();
        _repository.Update(measurement);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(serviceOrderId, measurementId);
    }

    public async Task<ServiceMeasurementDto?> ApproveAsync(
        Guid serviceOrderId,
        Guid measurementId,
        Guid approvedByUserId)
    {
        var serviceOrder = await GetServiceOrderOrThrowAsync(serviceOrderId);
        var measurement = await _repository.GetByServiceOrderAndIdAsync(
            serviceOrderId,
            measurementId);

        if (measurement == null)
            return null;

        await EnsureUserExistsAsync(approvedByUserId);
        await EnsureApprovedAmountFitsContractAsync(serviceOrder, measurement);

        measurement.Approve(approvedByUserId);

        var approvedAmount = await _repository.SumByServiceOrderAsync(
            serviceOrder.Id,
            ServiceMeasurementStatus.Approved);

        serviceOrder.RefreshExecutionByApprovedAmount(
            approvedAmount + measurement.Amount);

        _repository.Update(measurement);
        _serviceOrderRepository.Update(serviceOrder);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(serviceOrderId, measurementId);
    }

    public async Task<ServiceMeasurementDto?> RejectAsync(
        Guid serviceOrderId,
        Guid measurementId,
        RejectServiceMeasurementDto dto)
    {
        var serviceOrder = await GetServiceOrderOrThrowAsync(serviceOrderId);
        var measurement = await _repository.GetByServiceOrderAndIdAsync(
            serviceOrderId,
            measurementId);

        if (measurement == null)
            return null;

        await EnsureUserExistsAsync(dto.RejectedByUserId);

        measurement.Reject(dto.RejectedByUserId, dto.RejectionReason);

        var approvedAmount = await _repository.SumByServiceOrderAsync(
            serviceOrder.Id,
            ServiceMeasurementStatus.Approved);

        if (serviceOrder.ExecutionStatus == ServiceOrderExecutionStatus.Completed ||
            serviceOrder.ExecutionStatus == ServiceOrderExecutionStatus.InProgress)
        {
            serviceOrder.RefreshExecutionByApprovedAmount(approvedAmount);
        }

        _repository.Update(measurement);
        _serviceOrderRepository.Update(serviceOrder);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(serviceOrderId, measurementId);
    }

    private async Task<ServiceOrder> GetServiceOrderOrThrowAsync(Guid serviceOrderId)
    {
        var serviceOrder = await _serviceOrderRepository.GetWithDetailsByIdAsync(serviceOrderId);

        if (serviceOrder == null)
            throw new ArgumentException("A ordem de servico informada nao existe.");

        return serviceOrder;
    }

    private static void EnsureCanCreateMeasurement(ServiceOrder serviceOrder)
    {
        if (serviceOrder.ExecutionStatus != ServiceOrderExecutionStatus.Released &&
            serviceOrder.ExecutionStatus != ServiceOrderExecutionStatus.InProgress)
            throw new InvalidOperationException(
                "Apenas ordens de servico liberadas ou em execucao podem receber medicoes.");
    }

    private async Task EnsureUserExistsAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");
    }

    private async Task EnsureMeasurementFitsAsync(
        ServiceOrder serviceOrder,
        decimal quantity,
        decimal amount,
        Guid? ignoredMeasurementId = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("A quantidade medida deve ser maior que zero.");

        if (amount <= 0)
            throw new ArgumentException("O valor medido deve ser maior que zero.");

        var measurements = await _repository.GetByServiceOrderIdAsync(serviceOrder.Id);
        var committedAmount = measurements
            .Where(x => x.Id != ignoredMeasurementId &&
                x.Status != ServiceMeasurementStatus.Rejected)
            .Sum(x => x.Amount);

        if (committedAmount + amount > serviceOrder.CurrentContractedValue)
            throw new InvalidOperationException(
                "O valor medido ultrapassa o saldo disponivel da ordem de servico.");

        if (serviceOrder.CurrentContractedQuantity.HasValue)
        {
            var committedQuantity = measurements
                .Where(x => x.Id != ignoredMeasurementId && x.Status != ServiceMeasurementStatus.Rejected)
                .Sum(x => x.QuantityMeasured);

            if (committedQuantity + quantity > serviceOrder.CurrentContractedQuantity.Value)
                throw new InvalidOperationException("A quantidade medida ultrapassa a quantidade contratada da ordem de servico.");
        }
    }

    private async Task EnsureApprovedAmountFitsContractAsync(
        ServiceOrder serviceOrder,
        ServiceMeasurement measurement)
    {
        var approvedAmount = await _repository.SumByServiceOrderAsync(
            serviceOrder.Id,
            ServiceMeasurementStatus.Approved);

        if (approvedAmount + measurement.Amount > serviceOrder.CurrentContractedValue)
            throw new InvalidOperationException(
                "O valor aprovado ultrapassa o valor contratado da ordem de servico.");
    }

    private async Task<string> GenerateMeasurementNumberAsync(Guid serviceOrderId)
    {
        var existing = await _repository.GetByServiceOrderIdAsync(serviceOrderId);
        var sequence = existing.Count() + 1;

        while (existing.Any(x => x.MeasurementNumber == $"MED-{sequence:0000}"))
        {
            sequence++;
        }

        return $"MED-{sequence:0000}";
    }

    public static ServiceMeasurementDto MapToDto(ServiceMeasurement measurement)
    {
        return new ServiceMeasurementDto
        {
            Id = measurement.Id,
            ServiceOrderId = measurement.ServiceOrderId,
            MeasurementNumber = string.IsNullOrWhiteSpace(measurement.MeasurementNumber)
                ? "-"
                : measurement.MeasurementNumber,
            MeasurementDate = measurement.MeasurementDate,
            Description = measurement.Description,
            QuantityMeasured = measurement.QuantityMeasured,
            Unit = measurement.Unit,
            Amount = measurement.Amount,
            Observation = measurement.Observation,
            Status = measurement.Status,
            CreatedByUserId = measurement.CreatedByUserId,
            CreatedByUserName = FormatUserName(measurement.CreatedByUser),
            CreatedAt = measurement.CreatedAt,
            ApprovedAt = measurement.ApprovedAt,
            ApprovedByUserId = measurement.ApprovedByUserId,
            ApprovedByUserName = FormatUserName(measurement.ApprovedByUser),
            RejectedAt = measurement.RejectedAt,
            RejectedByUserId = measurement.RejectedByUserId,
            RejectedByUserName = FormatUserName(measurement.RejectedByUser),
            RejectionReason = measurement.RejectionReason,
            Attachments = measurement.Attachments.OrderBy(x => x.UploadedAt).Select(x => new ServiceMeasurementAttachmentDto
            {
                Id = x.Id,
                ServiceMeasurementId = x.ServiceMeasurementId,
                OriginalFileName = x.OriginalFileName,
                ContentType = x.ContentType,
                FileSizeBytes = x.FileSizeBytes,
                UploadedByUserId = x.UploadedByUserId,
                UploadedAt = x.UploadedAt
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
}
