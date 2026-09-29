using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.ToTable("Approvals");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Observation)
            .HasMaxLength(500);

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ApprovalDate)
            .IsRequired(false);

        builder.HasOne(x => x.PurchaseRequest)
            .WithMany()
            .HasForeignKey(x => x.PurchaseRequestId)
            .IsRequired(false);

        builder.HasOne(x => x.Quotation)
            .WithMany()
            .HasForeignKey(x => x.QuotationId)
            .IsRequired(false);

        builder.HasOne(x => x.ServiceOrderAmendment)
            .WithMany(x => x.Approvals)
            .HasForeignKey(x => x.ServiceOrderAmendmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);
    }
}
