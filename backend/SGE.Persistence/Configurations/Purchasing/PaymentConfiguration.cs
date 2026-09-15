using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.PaymentMethod)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TransactionReference)
            .HasMaxLength(100);

        builder.Property(x => x.InvoiceNumber)
            .HasMaxLength(50);

        builder.Property(x => x.InvoiceFileName)
            .HasMaxLength(255);

        builder.Property(x => x.InvoiceFilePath)
            .HasMaxLength(500);

        builder.Property(x => x.Observation)
            .HasMaxLength(500);

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.PurchaseOrderId);

        builder.HasOne(x => x.PaidByUser)
            .WithMany()
            .HasForeignKey(x => x.PaidByUserId);
    }
}
