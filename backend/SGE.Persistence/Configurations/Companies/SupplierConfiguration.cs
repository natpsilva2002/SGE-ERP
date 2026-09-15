using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Companies;

namespace SGE.Persistence.Configurations.Companies;
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

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

        builder.Property(x => x.StateRegistration)
            .HasMaxLength(30);

        builder.Property(x => x.Email)
            .HasMaxLength(150);

        builder.Property(x => x.Phone)
            .HasMaxLength(20);

        builder.Property(x => x.ContactName)
            .HasMaxLength(150);

        builder.Property(x => x.Address)
            .HasMaxLength(200);

        builder.Property(x => x.Number)
            .HasMaxLength(20);

        builder.Property(x => x.Complement)
            .HasMaxLength(100);

        builder.Property(x => x.District)
            .HasMaxLength(100);

        builder.Property(x => x.City)
            .HasMaxLength(100);

        builder.Property(x => x.State)
            .HasMaxLength(2);

        builder.Property(x => x.ZipCode)
            .HasMaxLength(10);

        builder.Property(x => x.PixKey)
            .HasMaxLength(150);

        builder.Property(x => x.Bank)
            .HasMaxLength(100);

        builder.Property(x => x.Agency)
            .HasMaxLength(20);

        builder.Property(x => x.Account)
            .HasMaxLength(30);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => x.Document)
            .IsUnique();

        builder.HasOne(x => x.Company)
            .WithMany(x => x.Suppliers)
            .HasForeignKey(x => x.CompanyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
