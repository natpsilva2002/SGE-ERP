using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityOrdered)
            .HasColumnType("numeric(18,4)");

        builder.Property(x => x.QuantityReceived)
            .HasColumnType("numeric(18,4)");

        builder.Property(x => x.Unit)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnType("numeric(18,4)");

        builder.Property(x => x.NegotiatedTotalValue)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.Observation)
            .HasMaxLength(500);

        builder.Ignore(x => x.TotalValue);
        builder.Ignore(x => x.QuantityPending);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId);

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseOrderId);
    }
}
