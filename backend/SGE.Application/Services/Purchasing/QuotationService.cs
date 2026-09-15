using SGE.Application.DTOs.Approval;
using SGE.Application.DTOs.PurchaseOrder;
using SGE.Application.DTOs.Quotation;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Application.Services.Purchasing;

public class QuotationService : IQuotationService
{
    private readonly IQuotationRepository _repository;
    private readonly IPurchaseRequestRepository _purchaseRequestRepository;
    private readonly IPurchaseRequestItemRepository _purchaseRequestItemRepository;
    private readonly IQuotationItemRepository _quotationItemRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IApprovalHistoryRepository _approvalHistoryRepository;
    private readonly IUserRepository _userRepository;

    public QuotationService(
        IQuotationRepository repository,
        IPurchaseRequestRepository purchaseRequestRepository,
        IPurchaseRequestItemRepository purchaseRequestItemRepository,
        IQuotationItemRepository quotationItemRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierRepository supplierRepository,
        IApprovalRepository approvalRepository,
        IApprovalHistoryRepository approvalHistoryRepository,
        IUserRepository userRepository)
    {
        _repository = repository;
        _purchaseRequestRepository = purchaseRequestRepository;
        _purchaseRequestItemRepository = purchaseRequestItemRepository;
        _quotationItemRepository = quotationItemRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _supplierRepository = supplierRepository;
        _approvalRepository = approvalRepository;
        _approvalHistoryRepository = approvalHistoryRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<QuotationDto>> GetAllAsync()
    {
        var quotations = await _repository.GetAllWithApprovalsAsync();

        return quotations.Select(MapToDto);
    }

    public async Task<QuotationDto?> GetByIdAsync(Guid id)
    {
        var quotation = await _repository.GetByIdWithApprovalsAsync(id);

        if (quotation == null)
            return null;

        return MapToDto(quotation);
    }

    public async Task<QuotationDto> CreateAsync(CreateQuotationDto dto)
    {
        var purchaseRequest = await _purchaseRequestRepository
            .GetByIdAsync(dto.PurchaseRequestId);

        if (purchaseRequest == null)
            throw new ArgumentException(
                "A solicitacao de compra informada nao existe.");

        if (purchaseRequest.Type != PurchaseRequestType.Material)
            throw new InvalidOperationException(
                "Apenas solicitacoes de material podem gerar cotacao.");

        if (purchaseRequest.Status != PurchaseRequestStatus.WaitingQuotation &&
            purchaseRequest.Status != PurchaseRequestStatus.QuotationInProgress)
            throw new InvalidOperationException(
                "A solicitacao de compra precisa estar aguardando cotacao ou em cotacao para criar uma cotacao.");

        var quotation = new Quotation(
            dto.PurchaseRequestId,
            await GenerateQuotationNumberAsync());

        await _repository.AddAsync(quotation);
        purchaseRequest.MarkQuotationInProgress();
        _purchaseRequestRepository.Update(purchaseRequest);
        await _repository.SaveChangesAsync();

        return MapToDto(quotation);
    }

    public async Task<QuotationDto?> UpdateAsync(
        Guid id,
        UpdateQuotationDto dto)
    {
        var quotation = await _repository.GetByIdWithApprovalsAsync(id);

        if (quotation == null)
            return null;

        quotation.Update(dto.Observation);

        _repository.Update(quotation);
        await _repository.SaveChangesAsync();

        return MapToDto(quotation);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var quotation = await _repository.GetByIdAsync(id);

        if (quotation == null)
            return false;

        _repository.Remove(quotation);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<QuotationDto?> SubmitForApprovalAsync(Guid id)
    {
        var quotation = await _repository.GetByIdAsync(id);

        if (quotation == null)
            return null;

        await ValidateSubmitForApprovalAsync(quotation);

        quotation.SubmitForApproval();

        _repository.Update(quotation);
        await _repository.SaveChangesAsync();

        return MapToDto(quotation);
    }

    public async Task<QuotationDto?> SelectSupplierAsync(
        Guid quotationId,
        Guid supplierId)
    {
        var quotation = await _repository.GetByIdAsync(quotationId);

        if (quotation == null)
            return null;

        await SelectSupplierItemsAsync(quotation, supplierId);
        await _repository.SaveChangesAsync();

        return MapToDto(quotation);
    }

    public async Task<QuotationApprovalResultDto?> ApproveAsync(
        Guid id,
        ApprovalDecisionDto dto)
    {
        var quotation = await _repository.GetByIdAsync(id);

        if (quotation == null)
            return null;

        var user = await _userRepository.GetByIdAsync(dto.UserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        var purchaseRequest = await _purchaseRequestRepository
            .GetByIdAsync(quotation.PurchaseRequestId);

        if (purchaseRequest == null)
            throw new InvalidOperationException(
                "A solicitacao de compra vinculada a cotacao nao existe.");

        var selectedItems = await ValidateApprovalAsync(quotation);
        var isSecondApproval = quotation.IsWaitingFinalApproval();

        if (isSecondApproval &&
            await _purchaseOrderRepository.ExistsActiveForPurchaseRequestAsync(purchaseRequest.Id))
            throw new InvalidOperationException(
                "Esta solicitacao de compra ja possui ordem de compra gerada por uma cotacao aprovada.");

        var approval = new Approval(
            ApprovalType.Quotation,
            null,
            quotation.Id,
            dto.UserId,
            ApprovalStatus.Pending,
            null);

        quotation.Approve(dto.UserId);
        approval.Approve(dto.UserId, dto.Observation);

        var history = new ApprovalHistory(
            approval.Id,
            dto.UserId,
            approval.Status,
            dto.Observation);

        var purchaseOrders = new List<PurchaseOrder>();

        await _approvalRepository.AddAsync(approval);
        await _approvalHistoryRepository.AddAsync(history);

        if (isSecondApproval)
        {
            purchaseOrders = CreatePurchaseOrders(quotation, selectedItems);
            var generationHistory = new ApprovalHistory(
                approval.Id,
                dto.UserId,
                approval.Status,
                BuildPurchaseOrderGenerationObservation(purchaseOrders));

            purchaseRequest.MarkPurchaseOrderGenerated();
            await _approvalHistoryRepository.AddAsync(generationHistory);

            foreach (var purchaseOrder in purchaseOrders)
            {
                await _purchaseOrderRepository.AddAsync(purchaseOrder);
            }

            _purchaseRequestRepository.Update(purchaseRequest);
        }

        _repository.Update(quotation);
        await _repository.SaveChangesAsync();

        return new QuotationApprovalResultDto
        {
            Quotation = MapToDto(await _repository.GetByIdWithApprovalsAsync(quotation.Id) ?? quotation),
            PurchaseOrders = purchaseOrders.Select(MapPurchaseOrderToDto)
        };
    }

    public async Task<QuotationDto?> RejectAsync(
        Guid id,
        ApprovalDecisionDto dto)
    {
        return await DecideAsync(
            id,
            dto,
            ApprovalStatus.Rejected);
    }

    private async Task<QuotationDto?> DecideAsync(
        Guid id,
        ApprovalDecisionDto dto,
        ApprovalStatus status)
    {
        var quotation = await _repository.GetByIdAsync(id);

        if (quotation == null)
            return null;

        var user = await _userRepository.GetByIdAsync(dto.UserId);

        if (user == null)
            throw new ArgumentException("O usuario informado nao existe.");

        var approval = new Approval(
            ApprovalType.Quotation,
            null,
            quotation.Id,
            dto.UserId,
            ApprovalStatus.Pending,
            null);

        if (status == ApprovalStatus.Approved)
        {
            quotation.Approve(dto.UserId);
            approval.Approve(dto.UserId, dto.Observation);
        }
        else if (status == ApprovalStatus.Rejected)
        {
            quotation.Reject();
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

        _repository.Update(quotation);
        await _repository.SaveChangesAsync();

        return MapToDto(quotation);
    }

    private async Task<List<SelectedQuotationItem>> ValidateApprovalAsync(Quotation quotation)
    {
        var quotationItems = await GetQuotationItemsAsync(quotation.Id);

        if (!quotationItems.Any())
            throw new InvalidOperationException(
                "Nao e possivel aprovar uma cotacao sem propostas.");

        var purchaseRequestItems = (await _purchaseRequestItemRepository.FindAsync(
                x => x.PurchaseRequestId == quotation.PurchaseRequestId))
            .ToList();

        if (!purchaseRequestItems.Any())
            throw new InvalidOperationException(
                "Nao e possivel aprovar uma cotacao sem itens na solicitacao.");

        var selectedItems = quotationItems
            .Where(x => x.Selected)
            .ToList();

        var selectedSupplierIds = selectedItems
            .Select(x => x.SupplierId)
            .Distinct()
            .ToList();

        if (selectedSupplierIds.Count != 1)
            throw new InvalidOperationException(
                "A cotacao precisa possuir exatamente um orcamento selecionado antes da aprovacao.");

        var selectedSupplierId = selectedSupplierIds.Single();

        if (!SupplierQuotedAllRequestItems(
                quotationItems,
                purchaseRequestItems,
                selectedSupplierId))
            throw new InvalidOperationException(
                "O fornecedor selecionado precisa ter cotado todos os materiais da solicitacao.");

        if (selectedItems.Count != purchaseRequestItems.Count ||
            selectedItems.Any(x => x.SupplierId != selectedSupplierId) ||
            purchaseRequestItems.Any(x => selectedItems.All(y => y.PurchaseRequestItemId != x.Id)))
            throw new InvalidOperationException(
                "O orcamento selecionado precisa conter uma oferta selecionada para cada material da solicitacao.");

        var selectedWithRequestItems = new List<SelectedQuotationItem>();

        foreach (var selectedItem in selectedItems)
        {
            if (selectedItem.UnitPrice <= 0)
                throw new InvalidOperationException(
                    "Toda proposta selecionada deve possuir preco unitario maior que zero.");

            var supplier = await _supplierRepository.GetByIdAsync(selectedItem.SupplierId);

            if (supplier == null)
                throw new InvalidOperationException(
                    "Toda proposta selecionada deve possuir fornecedor valido.");

            var purchaseRequestItem = purchaseRequestItems
                .First(x => x.Id == selectedItem.PurchaseRequestItemId);

            selectedWithRequestItems.Add(new SelectedQuotationItem(
                selectedItem,
                purchaseRequestItem));
        }

        return selectedWithRequestItems;
    }

    private async Task ValidateSubmitForApprovalAsync(Quotation quotation)
    {
        var quotationItems = await GetQuotationItemsAsync(quotation.Id);

        if (!quotationItems.Any())
            throw new InvalidOperationException(
                "Nao e possivel enviar uma cotacao para aprovacao sem orcamentos.");

        var purchaseRequestItems = (await _purchaseRequestItemRepository.FindAsync(
                x => x.PurchaseRequestId == quotation.PurchaseRequestId))
            .ToList();

        if (!purchaseRequestItems.Any())
            throw new InvalidOperationException(
                "Nao e possivel enviar uma cotacao sem itens na solicitacao.");

        if (purchaseRequestItems.Any(x => quotationItems.All(y => y.PurchaseRequestItemId != x.Id)))
            throw new InvalidOperationException(
                "Todos os itens da solicitacao precisam possuir pelo menos um orcamento antes do envio para aprovacao.");

        foreach (var quotationItem in quotationItems)
        {
            if (quotationItem.UnitPrice <= 0 || quotationItem.TotalPrice <= 0)
                throw new InvalidOperationException(
                    "Todo orcamento deve possuir valor maior que zero.");

            if (quotationItem.DeliveryDays < 0)
                throw new InvalidOperationException(
                    "Todo orcamento deve possuir prazo de entrega valido.");

            if (string.IsNullOrWhiteSpace(quotationItem.PaymentCondition))
                throw new InvalidOperationException(
                    "Todo orcamento deve possuir condicao de pagamento.");

            if (quotationItem.InstallmentCount.HasValue && quotationItem.InstallmentCount <= 0)
                throw new InvalidOperationException(
                    "Todo orcamento deve possuir quantidade de parcelas valida.");

            var supplier = await _supplierRepository.GetByIdAsync(quotationItem.SupplierId);

            if (supplier == null || !supplier.IsActive)
                throw new InvalidOperationException(
                    "Todo orcamento deve possuir fornecedor ativo e valido.");
        }
    }

    private async Task SelectSupplierItemsAsync(
        Quotation quotation,
        Guid supplierId)
    {
        if (quotation.Status != QuotationStatus.WaitingApproval)
            throw new InvalidOperationException(
                "Apenas cotacoes aguardando aprovacao permitem selecionar orcamento vencedor.");

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);

        if (supplier == null)
            throw new ArgumentException("O fornecedor informado nao existe.");

        var quotationItems = await GetQuotationItemsAsync(quotation.Id);
        var supplierItems = quotationItems
            .Where(x => x.SupplierId == supplierId)
            .ToList();

        if (!supplierItems.Any())
            throw new InvalidOperationException(
                "Este fornecedor nao possui orcamento nesta cotacao.");

        var purchaseRequestItems = (await _purchaseRequestItemRepository.FindAsync(
                x => x.PurchaseRequestId == quotation.PurchaseRequestId))
            .ToList();

        if (!SupplierQuotedAllRequestItems(
                quotationItems,
                purchaseRequestItems,
                supplierId))
            throw new InvalidOperationException(
                "Este fornecedor nao cotou todos os materiais da solicitacao.");

        foreach (var quotationItem in quotationItems)
        {
            if (quotationItem.SupplierId == supplierId)
            {
                quotationItem.Select();
            }
            else
            {
                quotationItem.Unselect();
            }
        }
    }

    private static bool SupplierQuotedAllRequestItems(
        IEnumerable<QuotationItem> quotationItems,
        IEnumerable<PurchaseRequestItem> purchaseRequestItems,
        Guid supplierId)
    {
        var supplierRequestItemIds = quotationItems
            .Where(x => x.SupplierId == supplierId)
            .Select(x => x.PurchaseRequestItemId)
            .ToHashSet();

        return purchaseRequestItems.All(x => supplierRequestItemIds.Contains(x.Id));
    }

    private List<PurchaseOrder> CreatePurchaseOrders(
        Quotation quotation,
        List<SelectedQuotationItem> selectedItems)
    {
        var purchaseOrders = new List<PurchaseOrder>();
        var sequence = 1;

        foreach (var supplierGroup in selectedItems.GroupBy(x => x.QuotationItem.SupplierId))
        {
            var purchaseOrder = new PurchaseOrder(
                quotation.Id,
                supplierGroup.Key,
                GeneratePurchaseOrderNumber(sequence),
                null);

            purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);

            var firstSelectedItem = supplierGroup.First().QuotationItem;
            purchaseOrder.SetCommercialTerms(
                firstSelectedItem.DeliveryDays,
                firstSelectedItem.PaymentCondition,
                firstSelectedItem.InstallmentCount);

            foreach (var selectedItem in supplierGroup)
            {
                purchaseOrder.AddItem(
                    selectedItem.PurchaseRequestItem.ItemId,
                    selectedItem.PurchaseRequestItem.Quantity,
                    selectedItem.PurchaseRequestItem.Unit,
                    selectedItem.QuotationItem.UnitPrice,
                    selectedItem.QuotationItem.TotalPrice,
                    selectedItem.QuotationItem.Observation);
            }

            purchaseOrder.RecalculateTotal();
            purchaseOrders.Add(purchaseOrder);
            sequence++;
        }

        return purchaseOrders;
    }

    private static string GeneratePurchaseOrderNumber(int sequence)
    {
        var suffix = Guid.NewGuid()
            .ToString("N")[..4]
            .ToUpperInvariant();

        return $"PO-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{sequence:0000}-{suffix}";
    }

    private static string BuildPurchaseOrderGenerationObservation(
        IEnumerable<PurchaseOrder> purchaseOrders)
    {
        var numbers = purchaseOrders.Select(x => x.Number);

        return "PurchaseOrders geradas automaticamente: " +
            string.Join(", ", numbers);
    }

    private async Task<List<QuotationItem>> GetQuotationItemsAsync(Guid quotationId)
    {
        var quotationItems = await _quotationItemRepository.GetAllAsync();

        return quotationItems
            .Where(x => x.QuotationId == quotationId)
            .ToList();
    }

    private static QuotationDto MapToDto(Quotation quotation)
    {
        return new QuotationDto
        {
            Id = quotation.Id,
            PurchaseRequestId = quotation.PurchaseRequestId,
            Number = quotation.Number,
            QuotationDate = quotation.QuotationDate,
            Observation = quotation.Observation,
            Status = quotation.Status,
            FirstApprovedAt = quotation.FirstApprovedAt,
            FirstApprovedByUserId = quotation.FirstApprovedByUserId,
            FirstApprovedByUserName = FormatUserName(quotation.FirstApprovedByUser),
            SecondApprovedAt = quotation.SecondApprovedAt,
            SecondApprovedByUserId = quotation.SecondApprovedByUserId,
            SecondApprovedByUserName = FormatUserName(quotation.SecondApprovedByUser)
        };
    }

    private static PurchaseOrderDto MapPurchaseOrderToDto(PurchaseOrder purchaseOrder)
    {
        return new PurchaseOrderDto
        {
            Id = purchaseOrder.Id,
            QuotationId = purchaseOrder.QuotationId,
            SupplierId = purchaseOrder.SupplierId,
            SupplierName = purchaseOrder.Supplier?.TradeName ??
                purchaseOrder.Supplier?.CorporateName ??
                string.Empty,
            SupplierDocument = purchaseOrder.Supplier?.Document ?? string.Empty,
            PurchaseRequestId = purchaseOrder.Quotation?.PurchaseRequestId ?? Guid.Empty,
            PurchaseRequestNumber = purchaseOrder.Quotation?.PurchaseRequest?.Number ?? string.Empty,
            WorkId = purchaseOrder.Quotation?.PurchaseRequest?.WorkId ?? Guid.Empty,
            WorkName = purchaseOrder.Quotation?.PurchaseRequest?.Work?.Name ?? string.Empty,
            QuotationNumber = purchaseOrder.Quotation?.Number ?? string.Empty,
            Number = purchaseOrder.Number,
            IssueDate = purchaseOrder.IssueDate,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            Status = purchaseOrder.Status,
            TotalValue = purchaseOrder.TotalValue,
            AmountPaid = purchaseOrder.AmountPaid,
            AmountPending = purchaseOrder.AmountPending,
            PaymentStatus = purchaseOrder.PaymentStatus,
            DeliveryDays = purchaseOrder.DeliveryDays,
            PaymentCondition = purchaseOrder.PaymentCondition,
            InstallmentCount = purchaseOrder.InstallmentCount,
            ApprovedAt = purchaseOrder.ApprovedAt,
            ApprovedByUserId = purchaseOrder.ApprovedByUserId,
            FirstApprovedAt = purchaseOrder.FirstApprovedAt,
            FirstApprovedByUserId = purchaseOrder.FirstApprovedByUserId,
            FirstApprovedByUserName = FormatUserName(purchaseOrder.FirstApprovedByUser),
            SecondApprovedAt = purchaseOrder.SecondApprovedAt,
            SecondApprovedByUserId = purchaseOrder.SecondApprovedByUserId,
            SecondApprovedByUserName = FormatUserName(purchaseOrder.SecondApprovedByUser),
            SentAt = purchaseOrder.SentAt,
            SentByUserId = purchaseOrder.SentByUserId,
            PaymentApprovedAt = purchaseOrder.PaymentApprovedAt,
            PaymentApprovedByUserId = purchaseOrder.PaymentApprovedByUserId,
            PaymentApprovedByUserName = FormatUserName(purchaseOrder.PaymentApprovedByUser),
            IsPaymentApproved = purchaseOrder.PaymentApprovedAt.HasValue,
            Items = purchaseOrder.Items.Select(x => new PurchaseOrderItemDto
            {
                Id = x.Id,
                PurchaseOrderId = x.PurchaseOrderId,
                ItemId = x.ItemId,
                ItemDescription = x.Item?.Description ?? string.Empty,
                QuantityOrdered = x.QuantityOrdered,
                QuantityReceived = x.QuantityReceived,
                QuantityPending = x.QuantityPending,
                Unit = x.Unit,
                UnitPrice = x.UnitPrice,
                NegotiatedTotalValue = x.NegotiatedTotalValue,
                TotalValue = x.TotalValue,
                Observation = x.Observation
            })
        };
    }

    private static string? FormatUserName(SGE.Domain.Entities.Administration.User? user)
    {
        if (user == null)
            return null;

        var name = $"{user.FirstName} {user.LastName}".Trim();

        return string.IsNullOrWhiteSpace(name) ? user.Email : name;
    }

    private sealed record SelectedQuotationItem(
        QuotationItem QuotationItem,
        PurchaseRequestItem PurchaseRequestItem);

    private async Task<string> GenerateQuotationNumberAsync()
    {
        string number;

        do
        {
            var suffix = Guid.NewGuid()
                .ToString("N")[..4]
                .ToUpperInvariant();

            number = $"CT-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
        }
        while (await _repository.ExistsByNumberAsync(number));

        return number;
    }
}
