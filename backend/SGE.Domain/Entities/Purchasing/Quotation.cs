using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class Quotation : BaseSoftDeleteEntity
{
    public Guid PurchaseRequestId { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public DateTime QuotationDate { get; private set; }

    public string? Observation { get; private set; }

    public QuotationStatus Status { get; private set; }

    public DateTime? FirstApprovedAt { get; private set; }

    public Guid? FirstApprovedByUserId { get; private set; }

    public DateTime? SecondApprovedAt { get; private set; }

    public Guid? SecondApprovedByUserId { get; private set; }

    public PurchaseRequest PurchaseRequest { get; private set; } = null!;

    public User? FirstApprovedByUser { get; private set; }

    public User? SecondApprovedByUser { get; private set; }

    public ICollection<QuotationItem> Items { get; private set; } =
        new List<QuotationItem>();

    public ICollection<QuotationAttachment> Attachments { get; private set; } =
        new List<QuotationAttachment>();

    public ICollection<QuotationSupplierOffer> SupplierOffers { get; private set; } =
        new List<QuotationSupplierOffer>();

    public bool IsEditable => Status != QuotationStatus.Approved &&
        Status != QuotationStatus.Completed;

    private Quotation()
    {
    }

    public Quotation(
        Guid purchaseRequestId,
        string number)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("O numero da cotacao e obrigatorio.");

        PurchaseRequestId = purchaseRequestId;
        Number = number;
        QuotationDate = DateTime.UtcNow;
        Status = QuotationStatus.Draft;
    }

    public void Update(string? observation)
    {
        if (!IsEditable)
            throw new InvalidOperationException(
                "So e possivel alterar cotacoes antes da aprovacao efetiva.");

        Observation = observation;
    }

    public void SubmitForApproval()
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException(
                "Apenas cotacoes em rascunho podem ser enviadas para aprovacao.");

        Status = QuotationStatus.WaitingApproval;
    }

    public void Approve(Guid userId)
    {
        if (Status == QuotationStatus.WaitingApproval)
        {
            Status = QuotationStatus.WaitingSecondApproval;
            FirstApprovedAt = DateTime.UtcNow;
            FirstApprovedByUserId = userId;
            return;
        }

        if (Status == QuotationStatus.WaitingSecondApproval)
        {
            if (FirstApprovedByUserId == userId)
                throw new InvalidOperationException(
                    "O segundo aprovador deve ser diferente do primeiro aprovador.");

            Status = QuotationStatus.Approved;
            SecondApprovedAt = DateTime.UtcNow;
            SecondApprovedByUserId = userId;
            return;
        }

        throw new InvalidOperationException(
            "Apenas cotacoes aguardando aprovacao podem ser aprovadas.");
    }

    public bool IsWaitingFinalApproval()
    {
        return Status == QuotationStatus.WaitingSecondApproval;
    }

    public void Reject()
    {
        if (Status != QuotationStatus.WaitingApproval &&
            Status != QuotationStatus.WaitingSecondApproval)
            throw new InvalidOperationException(
                "Apenas cotacoes aguardando aprovacao podem ser rejeitadas.");

        Status = QuotationStatus.Rejected;
    }

    public void Complete()
    {
        if (Status != QuotationStatus.Approved)
            throw new InvalidOperationException(
                "Apenas cotacoes aprovadas podem ser concluidas.");

        Status = QuotationStatus.Completed;
    }
}
