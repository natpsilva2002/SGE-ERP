using SGE.Domain.Common;
using SGE.Domain.Entities.Companies;

namespace SGE.Domain.Entities.Purchasing;

public class QuotationSupplierOffer : BaseEntity
{
    public Guid QuotationId { get; private set; }

    public Guid SupplierId { get; private set; }

    public decimal FreightValue { get; private set; }

    public Quotation Quotation { get; private set; } = null!;

    public Supplier Supplier { get; private set; } = null!;

    private QuotationSupplierOffer()
    {
    }

    public QuotationSupplierOffer(Guid quotationId, Guid supplierId, decimal freightValue)
    {
        ValidateFreightValue(freightValue);
        QuotationId = quotationId;
        SupplierId = supplierId;
        FreightValue = freightValue;
    }

    public void SetFreightValue(decimal freightValue)
    {
        ValidateFreightValue(freightValue);
        FreightValue = freightValue;
    }

    private static void ValidateFreightValue(decimal freightValue)
    {
        if (freightValue < 0)
            throw new ArgumentException("O valor do frete nao pode ser negativo.");
    }
}
