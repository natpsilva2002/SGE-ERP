using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TotalValue)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.FreightValue)
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.AmountPaid)
            .HasColumnType("numeric(18,2)");

        builder.Ignore(x => x.AmountPending);

        builder.Property(x => x.PaymentStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.PartialCloseReason)
            .HasMaxLength(500);

        builder.Property(x => x.PaymentCondition)
            .HasMaxLength(100);

        builder.HasOne(x => x.Quotation)
            .WithMany()
            .HasForeignKey(x => x.QuotationId);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId);

        builder.HasOne(x => x.FirstApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.FirstApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecondApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.SecondApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaymentApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.PaymentApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.PurchaseOrder)
            .HasForeignKey(x => x.PurchaseOrderId);

        builder.HasMany(x => x.Payments)
            .WithOne(x => x.PurchaseOrder)
            .HasForeignKey(x => x.PurchaseOrderId);
    }
}
