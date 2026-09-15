using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;
public class QuotationItemConfiguration : IEntityTypeConfiguration<QuotationItem>
{
    public void Configure(EntityTypeBuilder<QuotationItem> builder)
    {
        builder.ToTable("quotation_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18,4);

        builder.Property(x => x.TotalPrice)
            .HasPrecision(18,2);

        builder.Property(x => x.ProposalNumber)
            .HasMaxLength(60);

        builder.Property(x => x.PaymentCondition)
            .HasMaxLength(200);

        builder.Property(x => x.Observation)
            .HasMaxLength(500);

        builder.Property(x => x.InstallmentCount);

        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.QuotationId);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId);

        builder.HasOne(x => x.PurchaseRequestItem)
            .WithMany()
            .HasForeignKey(x => x.PurchaseRequestItemId);
    }
}
