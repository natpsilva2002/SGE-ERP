using SGE.Application.DTOs.ServiceOrder;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Application.Services.Purchasing;

public class ServiceOrderAmendmentService : IServiceOrderAmendmentService
{
    private readonly IServiceOrderAmendmentRepository _amendments;
    private readonly IServiceOrderRepository _orders;
    private readonly IApprovalHistoryRepository _histories;
    private readonly IUserRepository _users;

    public ServiceOrderAmendmentService(IServiceOrderAmendmentRepository amendments,
        IServiceOrderRepository orders, IApprovalHistoryRepository histories, IUserRepository users)
    {
        _amendments = amendments;
        _orders = orders;
        _histories = histories;
        _users = users;
    }

    public async Task<ServiceOrderAmendmentDto> CreateAsync(Guid orderId, Guid userId, CreateServiceOrderAmendmentDto dto)
    {
        var order = await GetReleasedOrderAsync(orderId);
        await EnsureUserAsync(userId);
        var amendment = new ServiceOrderAmendment(order.Id, userId, dto.Reason, dto.ValueAdjustment, dto.QuantityAdjustment, dto.Observation);
        await _amendments.AddAsync(amendment);
        await _amendments.SaveChangesAsync();
        return await MapAsync(amendment.Id) ?? throw new InvalidOperationException("Não foi possível carregar o adendo criado.");
    }

    public async Task<ServiceOrderAmendmentDto?> UpdateAsync(Guid orderId, Guid amendmentId, CreateServiceOrderAmendmentDto dto)
    {
        var amendment = await GetOwnAmendmentAsync(orderId, amendmentId);
        if (amendment == null) return null;
        amendment.Update(dto.Reason, dto.ValueAdjustment, dto.QuantityAdjustment, dto.Observation);
        _amendments.Update(amendment);
        await _amendments.SaveChangesAsync();
        return await MapAsync(amendmentId);
    }

    public async Task<ServiceOrderAmendmentDto?> SubmitAsync(Guid orderId, Guid amendmentId, Guid userId)
    {
        var amendment = await GetOwnAmendmentAsync(orderId, amendmentId);
        if (amendment == null) return null;
        await EnsureUserAsync(userId);
        await GetReleasedOrderAsync(orderId);
        amendment.Submit();
        var approval = new Approval(ApprovalType.ServiceOrderAmendment, null, null, userId,
            ApprovalStatus.Pending, null, amendment.Id);
        amendment.Approvals.Add(approval);
        await _amendments.SaveChangesAsync();
        return await MapAsync(amendment.Id);
    }

    public async Task<ServiceOrderAmendmentDto?> DecideAsync(Guid orderId, Guid amendmentId, Guid userId, bool approve, string? observation)
    {
        var amendment = await GetOwnAmendmentAsync(orderId, amendmentId);
        if (amendment == null) return null;
        await EnsureUserAsync(userId);
        var order = await GetReleasedOrderAsync(orderId);
        var approval = amendment.Approvals
            .Where(x => x.Status == ApprovalStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault() ?? throw new InvalidOperationException("Não existe aprovação pendente para este adendo.");
        if (approve)
        {
            amendment.Approve(userId, order);
            approval.Approve(userId, observation);
            var measuredAmount = order.Measurements.Where(x => x.Status == ServiceMeasurementStatus.Approved).Sum(x => x.Amount);
            order.RefreshExecutionByApprovedAmount(measuredAmount);
            order.RefreshPaymentStatus();
            _orders.Update(order);
        }
        else
        {
            amendment.Reject();
            approval.Reject(userId, observation);
        }
        await _histories.AddAsync(new ApprovalHistory(approval.Id, userId,
            approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected, observation));
        _amendments.Update(amendment);
        await _amendments.SaveChangesAsync();
        return await MapAsync(amendmentId);
    }

    public async Task<ServiceOrderAmendmentDto?> AddAttachmentAsync(Guid orderId, Guid amendmentId, string name, string path,
        string contentType, long size, Guid userId)
    {
        var amendment = await GetOwnAmendmentAsync(orderId, amendmentId);
        if (amendment == null) return null;
        if (amendment.Status is not (ServiceOrderAmendmentStatus.Draft or ServiceOrderAmendmentStatus.Rejected))
            throw new InvalidOperationException("Adendos enviados ou aprovados são somente leitura.");
        await EnsureUserAsync(userId);
        amendment.Attachments.Add(new ServiceOrderAmendmentAttachment(amendment.Id, name, path, contentType, size, userId));
        _amendments.Update(amendment);
        await _amendments.SaveChangesAsync();
        return await MapAsync(amendmentId);
    }

    public async Task<(bool Deleted, string? FilePath)> DeleteAttachmentAsync(Guid orderId, Guid amendmentId, Guid attachmentId)
    {
        var amendment = await GetOwnAmendmentAsync(orderId, amendmentId);
        if (amendment == null) return (false, null);
        if (amendment.Status is not (ServiceOrderAmendmentStatus.Draft or ServiceOrderAmendmentStatus.Rejected))
            throw new InvalidOperationException("Anexos de adendos enviados ou aprovados são somente leitura.");
        var attachment = amendment.Attachments.FirstOrDefault(x => x.Id == attachmentId);
        if (attachment == null) return (false, null);
        var path = attachment.FilePath;
        amendment.Attachments.Remove(attachment);
        _amendments.Update(amendment);
        await _amendments.SaveChangesAsync();
        return (true, path);
    }

    public async Task<(string FilePath, string FileName, string ContentType)?> GetAttachmentAsync(Guid orderId, Guid amendmentId, Guid attachmentId)
    {
        var amendment = await GetOwnAmendmentAsync(orderId, amendmentId);
        var attachment = amendment?.Attachments.FirstOrDefault(x => x.Id == attachmentId);
        return attachment == null ? null : (attachment.FilePath, attachment.OriginalFileName, attachment.ContentType);
    }

    private async Task<ServiceOrder> GetReleasedOrderAsync(Guid orderId)
    {
        var order = await _orders.GetWithDetailsByIdAsync(orderId)
            ?? throw new ArgumentException("A Ordem de Serviço informada não existe.");
        if (order.ExecutionStatus is ServiceOrderExecutionStatus.WaitingContract or ServiceOrderExecutionStatus.Cancelled)
            throw new InvalidOperationException("Adendos só podem ser registrados em Ordens de Serviço liberadas para execução.");
        return order;
    }

    private async Task<ServiceOrderAmendment?> GetOwnAmendmentAsync(Guid orderId, Guid amendmentId)
    {
        var amendment = await _amendments.GetWithDetailsAsync(amendmentId);
        return amendment?.ServiceOrderId == orderId ? amendment : null;
    }

    private async Task EnsureUserAsync(Guid userId)
    {
        if (await _users.GetByIdAsync(userId) == null)
            throw new ArgumentException("O usuário informado não existe.");
    }

    private async Task<ServiceOrderAmendmentDto?> MapAsync(Guid amendmentId)
    {
        var amendment = await _amendments.GetWithDetailsAsync(amendmentId);
        if (amendment == null) return null;
        var approval = amendment.Approvals.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        return Map(amendment, approval);
    }

    public static ServiceOrderAmendmentDto Map(ServiceOrderAmendment amendment, Approval? approval = null) => new()
    {
        Id = amendment.Id,
        ServiceOrderId = amendment.ServiceOrderId,
        Reason = amendment.Reason,
        ValueAdjustment = amendment.ValueAdjustment,
        QuantityAdjustment = amendment.QuantityAdjustment,
        Observation = amendment.Observation,
        Status = amendment.Status,
        CreatedByUserId = amendment.CreatedByUserId,
        CreatedByUserName = FormatUserName(amendment.CreatedByUser),
        CreatedAt = amendment.CreatedAt,
        ApprovedAt = amendment.ApprovedAt,
        ApprovedByUserId = amendment.ApprovedByUserId,
        ApprovedByUserName = FormatUserName(amendment.ApprovedByUser),
        ValueBeforeApproval = amendment.ValueBeforeApproval,
        ValueAfterApproval = amendment.ValueAfterApproval,
        QuantityBeforeApproval = amendment.QuantityBeforeApproval,
        QuantityAfterApproval = amendment.QuantityAfterApproval,
        ApprovalStatus = approval?.Status,
        Attachments = amendment.Attachments.OrderBy(x => x.UploadedAt).Select(x => new ServiceOrderAmendmentAttachmentDto
        {
            Id = x.Id,
            ServiceOrderAmendmentId = x.ServiceOrderAmendmentId,
            OriginalFileName = x.OriginalFileName,
            ContentType = x.ContentType,
            FileSizeBytes = x.FileSizeBytes,
            UploadedByUserId = x.UploadedByUserId,
            UploadedAt = x.UploadedAt
        }).ToList()
    };

    private static string? FormatUserName(SGE.Domain.Entities.Administration.User? user) => user == null
        ? null : $"{user.FirstName} {user.LastName}".Trim() is { Length: > 0 } name ? name : user.Email;
}
