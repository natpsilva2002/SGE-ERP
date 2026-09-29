using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class ServiceOrderAmendmentConfiguration : IEntityTypeConfiguration<ServiceOrderAmendment>
{
    public void Configure(EntityTypeBuilder<ServiceOrderAmendment> builder)
    {
        builder.ToTable("ServiceOrderAmendments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ValueAdjustment).HasColumnType("numeric(18,2)");
        builder.Property(x => x.QuantityAdjustment).HasColumnType("numeric(18,4)");
        builder.Property(x => x.Observation).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ValueBeforeApproval).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ValueAfterApproval).HasColumnType("numeric(18,2)");
        builder.Property(x => x.QuantityBeforeApproval).HasColumnType("numeric(18,4)");
        builder.Property(x => x.QuantityAfterApproval).HasColumnType("numeric(18,4)");
        builder.HasOne(x => x.ServiceOrder).WithMany(x => x.Amendments)
            .HasForeignKey(x => x.ServiceOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
