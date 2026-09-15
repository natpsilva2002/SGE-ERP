using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class ReceiptItemConfiguration : IEntityTypeConfiguration<ReceiptItem>
{
    public void Configure(EntityTypeBuilder<ReceiptItem> builder)
    {
        builder.ToTable("ReceiptItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityReceived)
            .HasColumnType("numeric(18,4)");

        builder.Property(x => x.Observation)
            .HasMaxLength(500);

        builder.Property(x => x.DivergenceQuantity)
            .HasColumnType("numeric(18,4)");

        builder.HasOne(x => x.PurchaseOrderItem)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderItemId);
    }
}
