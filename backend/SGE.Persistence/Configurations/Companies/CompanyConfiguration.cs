using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Companies;
namespace SGE.Persistence.Configurations.Companies;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CorporateName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TradeName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Document)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(150);

        builder.Property(x => x.Phone)
            .HasMaxLength(20);

        builder.HasIndex(x => x.Document)
            .IsUnique();
    }
}
