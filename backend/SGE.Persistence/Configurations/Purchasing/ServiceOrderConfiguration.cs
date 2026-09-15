using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("ServiceOrders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number)
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(x => x.Number)
            .IsUnique();

        builder.HasIndex(x => x.PurchaseRequestId)
            .IsUnique();

        builder.Property(x => x.ServiceDescription)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ServiceSpecification)
            .HasMaxLength(1000);

        builder.Property(x => x.EstimatedQuantity)
            .HasColumnType("numeric(18,4)");

        builder.Property(x => x.Unit)
            .HasMaxLength(20);

        builder.Property(x => x.ContractedValue)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.PaymentCondition)
            .HasMaxLength(500);

        builder.Property(x => x.InstallmentCount);

        builder.Property(x => x.ContractFileName)
            .HasMaxLength(255);

        builder.Property(x => x.ContractFilePath)
            .HasMaxLength(500);

        builder.Property(x => x.ContractUploadedAt);

        builder.Property(x => x.ExecutionStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.AmountPaid)
            .HasColumnType("numeric(18,2)");

        builder.Ignore(x => x.AmountPending);

        builder.Property(x => x.PaymentStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(x => x.PurchaseRequest)
            .WithMany()
            .HasForeignKey(x => x.PurchaseRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Work)
            .WithMany()
            .HasForeignKey(x => x.WorkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ContractUploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.ContractUploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Measurements)
            .WithOne(x => x.ServiceOrder)
            .HasForeignKey(x => x.ServiceOrderId);
    }
}
