using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Observation)
            .HasMaxLength(500);

        builder.Property(x => x.InvoiceNumber)
            .HasMaxLength(60);

        builder.Property(x => x.InvoiceFileName)
            .HasMaxLength(255);

        builder.Property(x => x.InvoiceFilePath)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Receipts)
            .HasForeignKey(x => x.PurchaseOrderId);

        builder.HasOne(x => x.ReceivedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReceivedByUserId);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Receipt)
            .HasForeignKey(x => x.ReceiptId);
    }
}
