using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class PaymentAttachmentConfiguration : IEntityTypeConfiguration<PaymentAttachment>
{
    public void Configure(EntityTypeBuilder<PaymentAttachment> builder)
    {
        builder.ToTable("PaymentAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.FileSizeBytes).IsRequired();
        builder.HasOne(x => x.Payment).WithMany(x => x.Attachments).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
