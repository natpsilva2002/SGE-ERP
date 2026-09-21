using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Persistence.Configurations.Purchasing;
public class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("purchase_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ServiceSpecification)
            .HasMaxLength(1000);

        builder.Property(x => x.ServiceQuantity)
            .HasColumnType("numeric(18,4)");

        builder.Property(x => x.ServiceUnit)
            .HasMaxLength(20);

        builder.HasIndex(x => x.ServiceUnitOfMeasureId);

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SGE.Domain.Entities.Catalog.UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.ServiceUnitOfMeasureId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Work)
            .WithMany()
            .HasForeignKey(x => x.WorkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
