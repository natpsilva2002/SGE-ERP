using SGE.Application.DTOs.Approval;
using SGE.Application.DTOs.PurchaseRequest;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Repositories.Catalog;
using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Application.Services.Purchasing;

public class PurchaseRequestService : IPurchaseRequestService
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IUserRepository _userRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IPurchaseRequestItemRepository _purchaseRequestItemRepository;
    private readonly IQuotationRepository _quotationRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IApprovalHistoryRepository _approvalHistoryRepository;

    public PurchaseRequestService(
        IPurchaseRequestRepository repository,
        ICompanyRepository companyRepository,
        IWorkRepository workRepository,
        IUserRepository userRepository,
        IItemRepository itemRepository,
        IPurchaseRequestItemRepository purchaseRequestItemRepository,
        IQuotationRepository quotationRepository,
        IApprovalRepository approvalRepository,
        IApprovalHistoryRepository approvalHistoryRepository)
    {
        _repository = repository;
        _companyRepository = companyRepository;
        _workRepository = workRepository;
        _userRepository = userRepository;
        _itemRepository = itemRepository;
        _purchaseRequestItemRepository = purchaseRequestItemRepository;
        _quotationRepository = quotationRepository;
        _approvalRepository = approvalRepository;
        _approvalHistoryRepository = approvalHistoryRepository;
    }

    public async Task<IEnumerable<PurchaseRequestDto>> GetAllAsync()
    {
        var purchaseRequests = await _repository.GetAllWithDetailsAsync();

        return purchaseRequests.Select(MapToDto);
    }

    public async Task<PurchaseRequestDto?> GetByIdAsync(Guid id)
    {
        var purchaseRequest = await _repository.GetByIdWithDetailsAsync(id);

        if (purchaseRequest == null)
            return null;

        return MapToDto(purchaseRequest);
    }

    public async Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestDto dto)
    {
        var work = await _workRepository.GetByIdAsync(dto.WorkId);

        if (work == null)
            throw new ArgumentException("A obra informada nao existe.");

        var companyId = dto.CompanyId ?? work.CompanyId;

        var company = await _companyRepository.GetByIdAsync(companyId);

        if (company == null)
            throw new ArgumentException("A empresa vinculada a obra nao existe.");

        if (!dto.RequestedByUserId.HasValue)
            throw new ArgumentException("O usuario solicitante nao foi identificado.");

        var user = await _userRepository.GetByIdAsync(dto.RequestedByUserId.Value);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        var number = string.IsNullOrWhiteSpace(dto.Number)
            ? await GenerateRequestNumberAsync()
            : dto.Number.Trim();

        if (dto.Type == PurchaseRequestType.Material &&
            string.IsNullOrWhiteSpace(dto.Description))
            throw new ArgumentException(
                "A descricao da solicitacao de material e obrigatoria.");

        if (dto.Type == PurchaseRequestType.Service &&
            string.IsNullOrWhiteSpace(dto.Description))
            throw new ArgumentException(
                "A descricao do servico e obrigatoria.");

        var purchaseRequest = new PurchaseRequest(
            companyId,
            dto.WorkId,
            dto.RequestedByUserId.Value,
            number,
            dto.Description.Trim(),
            dto.Type,
            dto.ServiceSpecification,
            dto.ServiceQuantity,
            dto.ServiceUnit);

        if (purchaseRequest.Type == PurchaseRequestType.Material)
        {
            var materialItems = dto.Items.ToList();

            if (!materialItems.Any())
                throw new ArgumentException(
                    "Adicione pelo menos um material a solicitacao.");

            purchaseRequest.RequestMaterial();
            await _repository.AddAsync(purchaseRequest);

            foreach (var materialItem in materialItems)
            {
                var item = await _itemRepository.GetByIdAsync(materialItem.ItemId);

                if (item == null)
                    throw new ArgumentException("O material informado nao existe.");

                if (!item.IsActive)
                    throw new ArgumentException("O material informado esta inativo.");

                if (materialItem.Quantity <= 0)
                    throw new ArgumentException("A quantidade deve ser maior que zero.");

                if (!IsWholeNumber(materialItem.Quantity))
                    throw new ArgumentException(
                        "A quantidade de material deve ser um numero inteiro.");

                var purchaseRequestItem = new PurchaseRequestItem(
                    purchaseRequest.Id,
                    item.Id,
                    materialItem.Quantity,
                    item.Unit,
                    materialItem.Observation);

                await _purchaseRequestItemRepository.AddAsync(purchaseRequestItem);
            }

            await _repository.SaveChangesAsync();

            return MapToDto(purchaseRequest);
        }

        await _repository.AddAsync(purchaseRequest);
        await _repository.SaveChangesAsync();

        return MapToDto(purchaseRequest);
    }

    public async Task<PurchaseRequestDto?> UpdateAsync(
        Guid id,
        UpdatePurchaseRequestDto dto)
    {
        var purchaseRequest = await _repository.GetByIdAsync(id);

        if (purchaseRequest == null)
            return null;

        if (purchaseRequest.Type == PurchaseRequestType.Material &&
            await _quotationRepository.ExistsForPurchaseRequestAsync(purchaseRequest.Id))
            throw new InvalidOperationException(
                "Esta solicitacao nao pode ser alterada porque ja possui cotacao vinculada.");

        if (purchaseRequest.Type == PurchaseRequestType.Material)
        {
            var workId = dto.WorkId ?? purchaseRequest.WorkId;
            var work = await _workRepository.GetByIdAsync(workId);

            if (work == null)
                throw new ArgumentException("A obra informada nao existe.");

            var company = await _companyRepository.GetByIdAsync(work.CompanyId);

            if (company == null)
                throw new ArgumentException("A empresa vinculada a obra nao existe.");

            purchaseRequest.UpdateMaterial(
                work.CompanyId,
                work.Id,
                dto.Description);

            _repository.Update(purchaseRequest);
            await _repository.SaveChangesAsync();

            return MapToDto(purchaseRequest);
        }

        purchaseRequest.Update(
            string.IsNullOrWhiteSpace(dto.Number) ? purchaseRequest.Number : dto.Number,
            dto.Description,
            dto.ServiceSpecification,
            dto.ServiceQuantity,
            dto.ServiceUnit);

        _repository.Update(purchaseRequest);
        await _repository.SaveChangesAsync();

        return MapToDto(purchaseRequest);
    }

    public async Task<PurchaseRequestDto?> SendToApprovalAsync(Guid id)
    {
        return await SubmitForApprovalAsync(id);
    }

    public async Task<PurchaseRequestDto?> SubmitForApprovalAsync(Guid id)
    {
        var purchaseRequest = await _repository.GetByIdAsync(id);

        if (purchaseRequest == null)
            return null;

        if (purchaseRequest.Type == PurchaseRequestType.Material)
            throw new InvalidOperationException(
                "Solicitacoes de material nao passam por aprovacao. Crie uma cotacao para aprovacao.");

        purchaseRequest.SendToApproval();

        _repository.Update(purchaseRequest);
        await _repository.SaveChangesAsync();

        return MapToDto(purchaseRequest);
    }

    public async Task<PurchaseRequestDto?> SendToQuotationAsync(Guid id)
    {
        var purchaseRequest = await _repository.GetByIdAsync(id);

        if (purchaseRequest == null)
            return null;

        purchaseRequest.SendToQuotation();

        _repository.Update(purchaseRequest);
        await _repository.SaveChangesAsync();

        return MapToDto(purchaseRequest);
    }

    public async Task<PurchaseRequestDto?> ApproveAsync(
        Guid id,
        ApprovalDecisionDto dto)
    {
        return await DecideAsync(id, dto, ApprovalStatus.Approved);
    }

    public async Task<PurchaseRequestDto?> RejectAsync(
        Guid id,
        ApprovalDecisionDto dto)
    {
        return await DecideAsync(id, dto, ApprovalStatus.Rejected);
    }

    private async Task<PurchaseRequestDto?> DecideAsync(
        Guid id,
        ApprovalDecisionDto dto,
        ApprovalStatus status)
    {
        var purchaseRequest = await _repository.GetByIdAsync(id);

        if (purchaseRequest == null)
            return null;

        var user = await _userRepository.GetByIdAsync(dto.UserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        var approval = new Approval(
            ApprovalType.PurchaseRequest,
            purchaseRequest.Id,
            null,
            dto.UserId,
            ApprovalStatus.Pending,
            null);

        if (status == ApprovalStatus.Approved)
        {
            purchaseRequest.Approve();
            approval.Approve(dto.UserId, dto.Observation);
        }
        else if (status == ApprovalStatus.Rejected)
        {
            purchaseRequest.Reject();
            approval.Reject(dto.UserId, dto.Observation);
        }
        else
        {
            throw new InvalidOperationException(
                "A decisao da aprovacao deve aprovar ou rejeitar.");
        }

        var history = new ApprovalHistory(
            approval.Id,
            dto.UserId,
            approval.Status,
            dto.Observation);

        await _approvalRepository.AddAsync(approval);
        await _approvalHistoryRepository.AddAsync(history);

        _repository.Update(purchaseRequest);
        await _repository.SaveChangesAsync();

        return MapToDto(purchaseRequest);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var purchaseRequest = await _repository.GetByIdAsync(id);

        if (purchaseRequest == null)
            return false;

        if (purchaseRequest.Type == PurchaseRequestType.Material &&
            await _quotationRepository.ExistsForPurchaseRequestAsync(purchaseRequest.Id))
            throw new InvalidOperationException(
                "Esta solicitacao nao pode ser excluida porque ja possui cotacao vinculada.");

        _repository.Remove(purchaseRequest);
        await _repository.SaveChangesAsync();

        return true;
    }

    private static PurchaseRequestDto MapToDto(PurchaseRequest purchaseRequest)
    {
        return new PurchaseRequestDto
        {
            Id = purchaseRequest.Id,
            CompanyId = purchaseRequest.CompanyId,
            WorkId = purchaseRequest.WorkId,
            RequestedByUserId = purchaseRequest.RequestedByUserId,
            RequestedByUserName = FormatUserName(purchaseRequest.RequestedByUser),
            Number = purchaseRequest.Number,
            Description = purchaseRequest.Description,
            CreatedAt = purchaseRequest.CreatedAt,
            Type = purchaseRequest.Type,
            ServiceSpecification = purchaseRequest.ServiceSpecification,
            ServiceQuantity = purchaseRequest.ServiceQuantity,
            ServiceUnit = purchaseRequest.ServiceUnit,
            Status = purchaseRequest.Status
        };
    }

    private static string? FormatUserName(SGE.Domain.Entities.Administration.User? user)
    {
        if (user == null)
            return null;

        var name = $"{user.FirstName} {user.LastName}".Trim();

        return string.IsNullOrWhiteSpace(name) ? user.Email : name;
    }

    private async Task<string> GenerateRequestNumberAsync()
    {
        string number;

        do
        {
            var suffix = Guid.NewGuid()
                .ToString("N")[..4]
                .ToUpperInvariant();

            number = $"SC-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
        }
        while (await _repository.ExistsByNumberAsync(number));

        return number;
    }

    private static bool IsWholeNumber(decimal value)
    {
        return value == decimal.Truncate(value);
    }
}
