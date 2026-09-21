using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class ServiceMeasurementAttachmentConfiguration : IEntityTypeConfiguration<ServiceMeasurementAttachment>
{
    public void Configure(EntityTypeBuilder<ServiceMeasurementAttachment> builder)
    {
        builder.ToTable("ServiceMeasurementAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.FileSizeBytes).IsRequired();
        builder.HasOne(x => x.ServiceMeasurement).WithMany(x => x.Attachments)
            .HasForeignKey(x => x.ServiceMeasurementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
