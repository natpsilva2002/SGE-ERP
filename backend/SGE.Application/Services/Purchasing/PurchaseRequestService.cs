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
    private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;
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
        IUnitOfMeasureRepository unitOfMeasureRepository,
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
        _unitOfMeasureRepository = unitOfMeasureRepository;
        _purchaseRequestItemRepository = purchaseRequestItemRepository;
        _quotationRepository = quotationRepository;
        _approvalRepository = approvalRepository;
        _approvalHistoryRepository = approvalHistoryRepository;
    }

    public async Task<IEnumerable<PurchaseRequestDto>> GetAllAsync()
    {
        var purchaseRequests = await _repository.GetAllWithDetailsAsync();
        var quotations = await _quotationRepository.GetAllWithApprovalsAsync();
        var quotationStatuses = quotations
            .GroupBy(x => x.PurchaseRequestId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.QuotationDate).First().Status);
        var quotationRequestIds = quotations.Select(x => x.PurchaseRequestId).ToHashSet();

        return purchaseRequests.OrderByDescending(x => x.CreatedAt)
            .Select(x => MapToDto(x, quotationStatuses.GetValueOrDefault(x.Id), quotationRequestIds.Contains(x.Id)));
    }

    public async Task<PurchaseRequestDto?> GetByIdAsync(Guid id)
    {
        var purchaseRequest = await _repository.GetByIdWithDetailsAsync(id);

        if (purchaseRequest == null)
            return null;

        var quotation = (await _quotationRepository.GetAllWithApprovalsAsync())
            .Where(x => x.PurchaseRequestId == id)
            .OrderByDescending(x => x.QuotationDate)
            .FirstOrDefault();

        return MapToDto(purchaseRequest, quotation?.Status, quotation != null);
    }

    public async Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestDto dto)
    {
        var work = await _workRepository.GetByIdAsync(dto.WorkId);

        if (work == null)
            throw new ArgumentException("A obra informada nao existe.");

        var company = await FindStructuralCompanyAsync();

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

        var serviceUnit = await ResolveServiceUnitAsync(dto.ServiceUnitOfMeasureId, dto.ServiceUnit);
        var purchaseRequest = new PurchaseRequest(
            company.Id,
            dto.WorkId,
            dto.RequestedByUserId.Value,
            number,
            dto.Description.Trim(),
            dto.Type,
            dto.ServiceSpecification,
            dto.ServiceQuantity,
            serviceUnit.Code,
            serviceUnit.Id);

        if (purchaseRequest.Type == PurchaseRequestType.Material)
        {
            var materialItems = dto.Items.ToList();

            if (!materialItems.Any())
                throw new ArgumentException(
                    "Adicione pelo menos um material a solicitacao.");

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

            return await GetByIdAsync(purchaseRequest.Id) ?? MapToDto(purchaseRequest);
        }

        await _repository.AddAsync(purchaseRequest);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(purchaseRequest.Id) ?? MapToDto(purchaseRequest);
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

            purchaseRequest.UpdateMaterial(work.Id, dto.Description);

            _repository.Update(purchaseRequest);
            await _repository.SaveChangesAsync();

            return MapToDto(purchaseRequest);
        }

        purchaseRequest.Update(
            string.IsNullOrWhiteSpace(dto.Number) ? purchaseRequest.Number : dto.Number,
            dto.Description,
            dto.ServiceSpecification,
            dto.ServiceQuantity,
            (await ResolveServiceUnitAsync(dto.ServiceUnitOfMeasureId, dto.ServiceUnit)).Code,
            dto.ServiceUnitOfMeasureId);

        _repository.Update(purchaseRequest);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(purchaseRequest.Id) ?? MapToDto(purchaseRequest);
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

        return await GetByIdAsync(purchaseRequest.Id) ?? MapToDto(purchaseRequest);
    }

    public async Task<PurchaseRequestDto?> SendToQuotationAsync(Guid id)
    {
        var purchaseRequest = await _repository.GetByIdAsync(id);

        if (purchaseRequest == null)
            return null;

        purchaseRequest.SendToQuotation();

        _repository.Update(purchaseRequest);
        await _repository.SaveChangesAsync();

        return await GetByIdAsync(purchaseRequest.Id) ?? MapToDto(purchaseRequest);
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

        if (purchaseRequest.Status == PurchaseRequestStatus.Approved ||
            purchaseRequest.Status == PurchaseRequestStatus.Rejected ||
            purchaseRequest.Status == PurchaseRequestStatus.PurchaseOrderGenerated ||
            purchaseRequest.Status == PurchaseRequestStatus.Finished ||
            purchaseRequest.Status == PurchaseRequestStatus.Cancelled)
            throw new InvalidOperationException("Solicitacoes aprovadas ou finalizadas nao podem ser excluidas.");

        if (purchaseRequest.Type == PurchaseRequestType.Material &&
            await _quotationRepository.ExistsForPurchaseRequestAsync(purchaseRequest.Id))
            throw new InvalidOperationException(
                "Esta solicitacao nao pode ser excluida porque ja possui cotacao vinculada.");

        _repository.Remove(purchaseRequest);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<PurchaseRequestDto?> CancelAsync(Guid id)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return null;
        if (request.Type != PurchaseRequestType.Service)
            throw new InvalidOperationException("Somente solicitacoes de servico podem ser canceladas neste fluxo.");
        request.Cancel();
        _repository.Update(request);
        await _repository.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    private static PurchaseRequestDto MapToDto(
        PurchaseRequest purchaseRequest,
        QuotationStatus? quotationStatus = null,
        bool hasQuotation = false)
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
            ServiceUnitOfMeasureId = purchaseRequest.ServiceUnitOfMeasureId,
            Status = purchaseRequest.Status
            ,WorkflowStatus = ResolveWorkflowStatus(purchaseRequest, quotationStatus)
            ,HasQuotation = hasQuotation
        };
    }

    private static string ResolveWorkflowStatus(
        PurchaseRequest request,
        QuotationStatus? quotationStatus)
    {
        if (request.Type == PurchaseRequestType.Service)
        {
            return request.Status switch
            {
                PurchaseRequestStatus.Draft => "Rascunho",
                PurchaseRequestStatus.WaitingApproval => "Aguardando aprovação",
                PurchaseRequestStatus.Approved => "Aprovada",
                PurchaseRequestStatus.Cancelled => "Cancelada",
                PurchaseRequestStatus.Rejected => "Rejeitada",
                _ => request.Status.ToString()
            };
        }

        return request.Status switch
        {
            PurchaseRequestStatus.Draft => "Rascunho",
            PurchaseRequestStatus.WaitingQuotation => "Aguardando cotação",
            PurchaseRequestStatus.QuotationInProgress when quotationStatus is QuotationStatus.Draft => "Cotação em andamento",
            PurchaseRequestStatus.QuotationInProgress when quotationStatus is QuotationStatus.WaitingApproval or QuotationStatus.WaitingSecondApproval => "Cotação aguardando aprovação",
            PurchaseRequestStatus.QuotationInProgress when quotationStatus is QuotationStatus.Approved => "Cotação aprovada",
            PurchaseRequestStatus.PurchaseOrderGenerated => "Ordem de compra gerada",
            PurchaseRequestStatus.Finished => "Finalizada",
            PurchaseRequestStatus.Cancelled => "Cancelada",
            PurchaseRequestStatus.Rejected => "Rejeitada",
            _ => request.Status.ToString()
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

    private async Task<(string? Code, Guid? Id)> ResolveServiceUnitAsync(Guid? unitId, string? legacyCode)
    {
        if (unitId.HasValue)
        {
            var unit = await _unitOfMeasureRepository.GetByIdAsync(unitId.Value);
            if (unit == null || !unit.IsActive)
                throw new ArgumentException("A unidade de medida informada nao existe ou esta inativa.");

            return (unit.Code, unit.Id);
        }

        return (string.IsNullOrWhiteSpace(legacyCode) ? null : legacyCode.Trim(), null);
    }

    private async Task<SGE.Domain.Entities.Companies.Company> FindStructuralCompanyAsync()
    {
        var companies = await _companyRepository.FindAsync(company =>
            company.TradeName == "Estrutural" ||
            company.CorporateName == "Estrutural");

        var company = companies.FirstOrDefault();

        if (company == null)
            throw new ArgumentException(
                "A empresa Estrutural nao esta cadastrada. Cadastre-a antes de criar solicitacoes.");

        return company;
    }
}
