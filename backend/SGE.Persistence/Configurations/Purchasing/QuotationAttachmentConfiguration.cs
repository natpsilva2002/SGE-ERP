using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;

namespace SGE.Persistence.Configurations.Purchasing;

public class QuotationAttachmentConfiguration : IEntityTypeConfiguration<QuotationAttachment>
{
    public void Configure(EntityTypeBuilder<QuotationAttachment> builder)
    {
        builder.ToTable("QuotationAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.FileSizeBytes).IsRequired();
        builder.HasOne(x => x.Quotation).WithMany(x => x.Attachments).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
