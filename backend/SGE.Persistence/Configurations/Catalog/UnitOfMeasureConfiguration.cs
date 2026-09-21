using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGE.Domain.Entities.Catalog;

namespace SGE.Persistence.Configurations.Catalog;

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("unit_of_measures");
        builder.HasKey(unit => unit.Id);
        builder.Property(unit => unit.Code).HasMaxLength(20).IsRequired();
        builder.Property(unit => unit.Description).HasMaxLength(120).IsRequired();
        builder.Ignore(unit => unit.IsActive);
        builder.HasIndex(unit => unit.Code).IsUnique();
    }
}
