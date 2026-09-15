using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class ServiceOrderPaymentConfiguration : IEntityTypeConfiguration<ServiceOrderPayment>
{
    public void Configure(EntityTypeBuilder<ServiceOrderPayment> builder)
    {
        builder.ToTable("ServiceOrderPayments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Observation)
            .HasMaxLength(500);

        builder.HasOne(x => x.ServiceOrder)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.ServiceOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaidByUser)
            .WithMany()
            .HasForeignKey(x => x.PaidByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AdvancePaymentRequest)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.AdvancePaymentRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
