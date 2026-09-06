using Microsoft.EntityFrameworkCore;
using SlotBook.Core;

namespace SlotBook.Infrastructure;

public class SlotBookDbContext(DbContextOptions<SlotBookDbContext> options) : DbContext(options)
{
    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<ReservationSlot> ReservationSlots => Set<ReservationSlot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SlotBookDbContext).Assembly);
    }
}
