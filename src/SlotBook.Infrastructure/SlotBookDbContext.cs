using Microsoft.EntityFrameworkCore;
using SlotBook.Core;

namespace SlotBook.Infrastructure;

public class SlotBookDbContext(DbContextOptions<SlotBookDbContext> options) : DbContext(options)
{
    public DbSet<Resource> Resources => Set<Resource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SlotBookDbContext).Assembly);
    }
}
