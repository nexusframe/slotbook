using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SlotBook.Core;

namespace SlotBook.Infrastructure.Configurations;

internal sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.Property(r => r.Name)
            .HasMaxLength(200);

        builder.HasIndex(r => r.Name)
            .IsUnique();

        builder.Property(r => r.Kind)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
