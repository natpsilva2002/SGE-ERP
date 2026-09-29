using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class ServiceOrderAmendmentAttachmentConfiguration : IEntityTypeConfiguration<ServiceOrderAmendmentAttachment>
{
    public void Configure(EntityTypeBuilder<ServiceOrderAmendmentAttachment> builder)
    {
        builder.ToTable("ServiceOrderAmendmentAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.FileSizeBytes).IsRequired();
        builder.Property(x => x.UploadedAt).IsRequired();
        builder.HasOne(x => x.Amendment).WithMany(x => x.Attachments)
            .HasForeignKey(x => x.ServiceOrderAmendmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
