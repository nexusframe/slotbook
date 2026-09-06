using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SlotBook.Core;

namespace SlotBook.Infrastructure.Configurations;

internal sealed class ReservationSlotConfiguration : IEntityTypeConfiguration<ReservationSlot>
{
    public void Configure(EntityTypeBuilder<ReservationSlot> builder)
    {
        // The rule itself. A primary key is clustered by default in SQL Server, so the table is
        // stored in the order it is read in, and the constraint needs no second index behind it.
        builder.HasKey(s => new { s.ResourceId, s.SlotIndex });
    }
}
