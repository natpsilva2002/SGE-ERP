using SGE.Domain.Common;
using SGE.Domain.Entities.Companies;

namespace SGE.Domain.Entities.Purchasing;

public class QuotationItem : BaseEntity
{
    public Guid QuotationId { get; private set; }

    public Guid PurchaseRequestItemId { get; private set; }

    public Guid SupplierId { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TotalPrice { get; private set; }

    public int DeliveryDays { get; private set; }

    public string? ProposalNumber { get; private set; }

    public string? PaymentCondition { get; private set; }

    public int? InstallmentCount { get; private set; }

    public string? Observation { get; private set; }

    public bool Selected { get; private set; }

    public Quotation Quotation { get; private set; } = null!;

    public PurchaseRequestItem PurchaseRequestItem { get; private set; } = null!;

    public Supplier Supplier { get; private set; } = null!;

    private QuotationItem()
    {
    }

    public QuotationItem(
        Guid quotationId,
        Guid purchaseRequestItemId,
        Guid supplierId,
        decimal unitPrice,
        decimal totalPrice,
        int deliveryDays,
        string? proposalNumber = null,
        string? paymentCondition = null,
        int? installmentCount = null,
        string? observation = null)
    {
        if (unitPrice <= 0)
            throw new ArgumentException(
                "O preço unitário deve ser maior que zero.");

        if (totalPrice <= 0)
            throw new ArgumentException(
                "O preco total deve ser maior que zero.");

        if (deliveryDays < 0)
            throw new ArgumentException(
                "O prazo de entrega não pode ser negativo.");

        if (installmentCount is <= 0)
            throw new ArgumentException(
                "A quantidade de parcelas deve ser maior que zero.");

        QuotationId = quotationId;
        PurchaseRequestItemId = purchaseRequestItemId;
        SupplierId = supplierId;
        UnitPrice = unitPrice;
        TotalPrice = totalPrice;
        DeliveryDays = deliveryDays;
        ProposalNumber = proposalNumber;
        PaymentCondition = paymentCondition;
        InstallmentCount = installmentCount;
        Observation = string.IsNullOrWhiteSpace(observation)
            ? null
            : observation.Trim();
    }

    public void Update(
        decimal unitPrice,
        decimal totalPrice,
        int deliveryDays,
        string? proposalNumber = null,
        string? paymentCondition = null,
        int? installmentCount = null,
        string? observation = null)
    {
        if (unitPrice <= 0)
            throw new ArgumentException(
                "O preço unitário deve ser maior que zero.");

        if (totalPrice <= 0)
            throw new ArgumentException(
                "O preco total deve ser maior que zero.");

        if (deliveryDays < 0)
            throw new ArgumentException(
                "O prazo de entrega não pode ser negativo.");

        if (installmentCount is <= 0)
            throw new ArgumentException(
                "A quantidade de parcelas deve ser maior que zero.");

        UnitPrice = unitPrice;
        TotalPrice = totalPrice;
        DeliveryDays = deliveryDays;
        ProposalNumber = proposalNumber;
        PaymentCondition = paymentCondition;
        InstallmentCount = installmentCount;
        Observation = string.IsNullOrWhiteSpace(observation)
            ? null
            : observation.Trim();
    }

    public void Select()
    {
        Selected = true;
    }

    public void Unselect()
    {
        Selected = false;
    }
}
