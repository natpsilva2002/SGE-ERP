using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Companies;

namespace SGE.Persistence.Configurations.Companies;
public class WorkConfiguration : IEntityTypeConfiguration<Work>
{
    public void Configure(EntityTypeBuilder<Work> builder)
    {
        builder.ToTable("works");

        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.IsActive);

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasOne(x => x.Company)
            .WithMany(x => x.Works)
            .HasForeignKey(x => x.CompanyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Code)
            .IsUnique();
    }
}
