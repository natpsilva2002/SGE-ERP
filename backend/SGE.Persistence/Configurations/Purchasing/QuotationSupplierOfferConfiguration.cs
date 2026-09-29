using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class QuotationSupplierOfferConfiguration : IEntityTypeConfiguration<QuotationSupplierOffer>
{
    public void Configure(EntityTypeBuilder<QuotationSupplierOffer> builder)
    {
        builder.ToTable("QuotationSupplierOffers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FreightValue)
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();
        builder.HasIndex(x => new { x.QuotationId, x.SupplierId })
            .IsUnique();
        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.SupplierOffers)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
