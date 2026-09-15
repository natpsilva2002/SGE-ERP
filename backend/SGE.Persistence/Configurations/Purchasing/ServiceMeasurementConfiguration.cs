using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class ServiceMeasurementConfiguration : IEntityTypeConfiguration<ServiceMeasurement>
{
    public void Configure(EntityTypeBuilder<ServiceMeasurement> builder)
    {
        builder.ToTable("ServiceMeasurements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MeasurementNumber)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.HasIndex(x => new { x.ServiceOrderId, x.MeasurementNumber })
            .IsUnique();

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.QuantityMeasured)
            .HasColumnType("numeric(18,4)");

        builder.Property(x => x.Unit)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.Observation)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RejectedByUser)
            .WithMany()
            .HasForeignKey(x => x.RejectedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
