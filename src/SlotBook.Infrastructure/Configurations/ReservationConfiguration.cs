using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SlotBook.Core;

namespace SlotBook.Infrastructure.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        // A complex property, not OwnsOne: a period has no identity of its own, so it belongs in
        // the owner's row without a tracking entry. The columns are flat either way.
        builder.ComplexProperty(r => r.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("StartsAt");
            period.Property(p => p.End).HasColumnName("EndsAt");
        });

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Written out because there is no navigation to Resource, and EF Core discovers
        // relationships from navigations rather than from column names. Without this the column
        // is a plain int and a reservation can name a resource that was never created.
        builder.HasOne<Resource>()
            .WithMany()
            .HasForeignKey(r => r.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
