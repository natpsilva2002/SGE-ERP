using SGE.Domain.Common;
using SGE.Domain.Enums;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Entities.Companies;

namespace SGE.Domain.Entities.Purchasing;

public class PurchaseRequest : BaseSoftDeleteEntity
{
    public Guid CompanyId { get; private set; }

    public Guid WorkId { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public PurchaseRequestType Type { get; private set; }

    public string? ServiceSpecification { get; private set; }

    public decimal? ServiceQuantity { get; private set; }

    public string? ServiceUnit { get; private set; }

    public PurchaseRequestStatus Status { get; private set; }

    public Company Company { get; private set; } = null!;

    public Work Work { get; private set; } = null!;

    public User RequestedByUser { get; private set; } = null!;

    public ICollection<PurchaseRequestItem> Items { get; private set; } = new List<PurchaseRequestItem>();

    private PurchaseRequest()
    {
    }

    public PurchaseRequest(
        Guid companyId,
        Guid workId,
        Guid requestedByUserId,
        string number,
        string description,
        PurchaseRequestType type = PurchaseRequestType.Material,
        string? serviceSpecification = null,
        decimal? serviceQuantity = null,
        string? serviceUnit = null)
    {
        CompanyId = companyId;
        WorkId = workId;
        RequestedByUserId = requestedByUserId;
        Number = number;
        Description = description;
        if (type != PurchaseRequestType.Material && type != PurchaseRequestType.Service)
            throw new ArgumentException("O tipo da solicitacao de compra e invalido.");

        Type = type;
        if (Type == PurchaseRequestType.Material && string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(
                "A descricao da solicitacao de material e obrigatoria.");

        if (Type == PurchaseRequestType.Service && string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(
                "A descricao do servico e obrigatoria.");

        SetServiceData(serviceSpecification, serviceQuantity, serviceUnit);
        Status = PurchaseRequestStatus.Draft;
    }

    public void SendToApproval()
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new InvalidOperationException(
                "Apenas solicitacoes em rascunho podem ser enviadas para aprovacao.");

        Status = PurchaseRequestStatus.WaitingApproval;
    }

    public void RequestMaterial()
    {
        if (Type != PurchaseRequestType.Material)
            throw new InvalidOperationException(
                "Apenas solicitacoes de material podem ser solicitadas por este fluxo.");

        if (Status != PurchaseRequestStatus.Draft)
            throw new InvalidOperationException(
                "Apenas solicitacoes de material em rascunho podem ser solicitadas.");

        Status = PurchaseRequestStatus.WaitingQuotation;
    }

    public void MarkQuotationInProgress()
    {
        if (Type != PurchaseRequestType.Material)
            throw new InvalidOperationException(
                "Apenas solicitacoes de material podem entrar em cotacao.");

        if (Status != PurchaseRequestStatus.WaitingQuotation &&
            Status != PurchaseRequestStatus.QuotationInProgress)
            throw new InvalidOperationException(
                "Apenas solicitacoes aguardando cotacao podem entrar em cotacao.");

        Status = PurchaseRequestStatus.QuotationInProgress;
    }

    public void SendToQuotation()
    {
        if (Type != PurchaseRequestType.Material)
            throw new InvalidOperationException(
                "Apenas solicitacoes de material podem ser enviadas para cotacao.");

        if (Status != PurchaseRequestStatus.Approved)
            throw new InvalidOperationException(
                "Apenas solicitacoes aprovadas podem ser enviadas para cotacao.");

        Status = PurchaseRequestStatus.WaitingQuotation;
    }

    public void Approve()
    {
        if (Status != PurchaseRequestStatus.WaitingApproval)
            throw new InvalidOperationException(
                "Apenas solicitacoes aguardando aprovacao podem ser aprovadas.");

        Status = PurchaseRequestStatus.Approved;
    }

    public void Reject()
    {
        if (Status != PurchaseRequestStatus.WaitingApproval)
            throw new InvalidOperationException(
                "Apenas solicitacoes aguardando aprovacao podem ser rejeitadas.");

        Status = PurchaseRequestStatus.Rejected;
    }

    public void Finish()
    {
        if (Status != PurchaseRequestStatus.WaitingQuotation &&
            Status != PurchaseRequestStatus.QuotationInProgress &&
            Status != PurchaseRequestStatus.PurchaseOrderGenerated)
            throw new InvalidOperationException(
                "Apenas solicitacoes em cotacao ou com pedido gerado podem ser finalizadas.");

        Status = PurchaseRequestStatus.Finished;
    }

    public void MarkPurchaseOrderGenerated()
    {
        if (Type != PurchaseRequestType.Material)
            throw new InvalidOperationException(
                "Apenas solicitacoes de material podem gerar ordem de compra.");

        if (Status != PurchaseRequestStatus.WaitingQuotation &&
            Status != PurchaseRequestStatus.QuotationInProgress)
            throw new InvalidOperationException(
                "Apenas solicitacoes aguardando cotacao podem gerar ordem de compra.");

        Status = PurchaseRequestStatus.PurchaseOrderGenerated;
    }

    public void Update(
        string number,
        string description,
        string? serviceSpecification = null,
        decimal? serviceQuantity = null,
        string? serviceUnit = null)
    {
        if (Status != PurchaseRequestStatus.Draft &&
            (Type != PurchaseRequestType.Material ||
             Status != PurchaseRequestStatus.WaitingQuotation))
            throw new InvalidOperationException(
                "So e possivel alterar solicitacoes em rascunho ou solicitacoes de material sem cotacao vinculada.");

        Number = number;
        Description = description;
        SetServiceData(serviceSpecification, serviceQuantity, serviceUnit);
    }

    public void UpdateMaterial(
        Guid companyId,
        Guid workId,
        string description)
    {
        if (Type != PurchaseRequestType.Material)
            throw new InvalidOperationException(
                "Este metodo altera apenas solicitacoes de material.");

        if (Status != PurchaseRequestStatus.Draft &&
            Status != PurchaseRequestStatus.WaitingQuotation)
            throw new InvalidOperationException(
                "So e possivel alterar solicitacoes de material antes de existir cotacao vinculada.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(
                "A descricao da solicitacao de material e obrigatoria.");

        CompanyId = companyId;
        WorkId = workId;
        Description = description.Trim();
        SetServiceData(null, null, null);
    }

    private void SetServiceData(
        string? serviceSpecification,
        decimal? serviceQuantity,
        string? serviceUnit)
    {
        if (Type == PurchaseRequestType.Material)
        {
            ServiceSpecification = null;
            ServiceQuantity = null;
            ServiceUnit = null;
            return;
        }

        if (serviceQuantity.HasValue && serviceQuantity.Value <= 0)
            throw new ArgumentException(
                "A quantidade prevista do servico deve ser maior que zero.");

        if (serviceQuantity.HasValue && string.IsNullOrWhiteSpace(serviceUnit))
            throw new ArgumentException(
                "A unidade de medicao do servico e obrigatoria quando houver quantidade prevista.");

        ServiceSpecification = serviceSpecification;
        ServiceQuantity = serviceQuantity;
        ServiceUnit = serviceUnit;
    }
}
