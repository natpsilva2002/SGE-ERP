using SGE.Application.DTOs.Approval;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Application.Services.Purchasing;

public class ApprovalService : IApprovalService
{
    private readonly IApprovalRepository _repository;
    private readonly IApprovalHistoryRepository _approvalHistoryRepository;
    private readonly IPurchaseRequestRepository _purchaseRequestRepository;
    private readonly IQuotationRepository _quotationRepository;
    private readonly IUserRepository _userRepository;

    public ApprovalService(
        IApprovalRepository repository,
        IApprovalHistoryRepository approvalHistoryRepository,
        IPurchaseRequestRepository purchaseRequestRepository,
        IQuotationRepository quotationRepository,
        IUserRepository userRepository)
    {
        _repository = repository;
        _approvalHistoryRepository = approvalHistoryRepository;
        _purchaseRequestRepository = purchaseRequestRepository;
        _quotationRepository = quotationRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<ApprovalDto>> GetAllAsync()
    {
        var approvals = await _repository.GetAllAsync();

        return approvals.Select(MapToDto);
    }

    public async Task<ApprovalDto?> GetByIdAsync(Guid id)
    {
        var approval = await _repository.GetByIdAsync(id);

        if (approval == null)
            return null;

        return MapToDto(approval);
    }

    public async Task<ApprovalDto> CreateAsync(CreateApprovalDto dto)
    {
        await ValidateUserAsync(dto.UserId);
        await ValidateTargetAsync(dto.Type, dto.PurchaseRequestId, dto.QuotationId);

        var approval = new Approval(
            dto.Type,
            dto.PurchaseRequestId,
            dto.QuotationId,
            dto.UserId,
            ApprovalStatus.Pending,
            null);

        if (dto.Status == ApprovalStatus.Approved)
            approval.Approve(dto.UserId, dto.Observation);
        else if (dto.Status == ApprovalStatus.Rejected)
            approval.Reject(dto.UserId, dto.Observation);
        else if (dto.Status != ApprovalStatus.Pending)
            throw new InvalidOperationException(
                "Status de aprovacao invalido.");

        await _repository.AddAsync(approval);

        if (approval.Status != ApprovalStatus.Pending)
            await AddHistoryAsync(approval);

        await _repository.SaveChangesAsync();

        return MapToDto(approval);
    }

    public async Task<ApprovalDto?> UpdateAsync(
        Guid id,
        UpdateApprovalDto dto)
    {
        var approval = await _repository.GetByIdAsync(id);

        if (approval == null)
            return null;

        if (dto.Status == ApprovalStatus.Approved)
            approval.Approve(approval.UserId, dto.Observation);
        else if (dto.Status == ApprovalStatus.Rejected)
            approval.Reject(approval.UserId, dto.Observation);
        else
            throw new InvalidOperationException(
                "A atualizacao de aprovacao deve aprovar ou rejeitar.");

        await AddHistoryAsync(approval);

        _repository.Update(approval);
        await _repository.SaveChangesAsync();

        return MapToDto(approval);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var approval = await _repository.GetByIdAsync(id);

        if (approval == null)
            return false;

        _repository.Remove(approval);
        await _repository.SaveChangesAsync();

        return true;
    }

    private async Task ValidateUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");
    }

    private async Task ValidateTargetAsync(
        ApprovalType type,
        Guid? purchaseRequestId,
        Guid? quotationId)
    {
        if (type == ApprovalType.PurchaseRequest)
        {
            if (!purchaseRequestId.HasValue || quotationId.HasValue)
                throw new ArgumentException(
                    "A aprovacao de solicitacao deve possuir apenas PurchaseRequestId.");

            var purchaseRequest = await _purchaseRequestRepository
                .GetByIdAsync(purchaseRequestId.Value);

            if (purchaseRequest == null)
                throw new ArgumentException(
                    "A solicitacao de compra informada nao existe.");
        }

        if (type == ApprovalType.Quotation)
        {
            if (!quotationId.HasValue || purchaseRequestId.HasValue)
                throw new ArgumentException(
                    "A aprovacao de cotacao deve possuir apenas QuotationId.");

            var quotation = await _quotationRepository
                .GetByIdAsync(quotationId.Value);

            if (quotation == null)
                throw new ArgumentException(
                    "A cotacao informada nao existe.");
        }

        if (type != ApprovalType.PurchaseRequest &&
            type != ApprovalType.Quotation)
            throw new ArgumentException(
                "Tipo de aprovacao invalido.");
    }

    private async Task AddHistoryAsync(Approval approval)
    {
        var history = new ApprovalHistory(
            approval.Id,
            approval.UserId,
            approval.Status,
            approval.Observation);

        await _approvalHistoryRepository.AddAsync(history);
    }

    private static ApprovalDto MapToDto(Approval approval)
    {
        return new ApprovalDto
        {
            Id = approval.Id,
            Type = approval.Type,
            Status = approval.Status,
            PurchaseRequestId = approval.PurchaseRequestId,
            QuotationId = approval.QuotationId,
            UserId = approval.UserId,
            Observation = approval.Observation,
            ApprovalDate = approval.ApprovalDate
        };
    }
}
