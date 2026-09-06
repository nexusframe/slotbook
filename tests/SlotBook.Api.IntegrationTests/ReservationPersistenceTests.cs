using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SlotBook.Core;
using SlotBook.Infrastructure;

using static SlotBook.Api.IntegrationTests.Instants;

namespace SlotBook.Api.IntegrationTests;

// About the schema, not about an endpoint. What is pinned here is what the database refuses,
// which no request through the API can reach once a handler starts checking things first.
[Collection(ApiCollectionDefinition.Name)]
public sealed class ReservationPersistenceTests(SlotBookApiFixture fixture)
{
    [Fact]
    public async Task A_reservation_and_its_quarter_hours_survive_a_round_trip()
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SlotBookDbContext>();

        var resourceId = await CreateResourceAsync(db, "Sala Owalna");
        var period = new TimeSlot(At(10), At(11));

        db.Reservations.Add(Reservation.For(resourceId, period));
        await db.SaveChangesAsync();

        // Without this the entity comes back out of the change tracker, still the instance that
        // was written, and the read would prove nothing about how the columns are mapped.
        db.ChangeTracker.Clear();

        var read = await db.Reservations
            .Include(r => r.Slots)
            .SingleAsync(r => r.ResourceId == resourceId);

        // Period is a readonly record struct with no setters, so this also answers whether EF
        // Core can rebuild it through the constructor when reading two columns back.
        Assert.Equal(period, read.Period);
        Assert.Equal(ReservationStatus.Confirmed, read.Status);
        Assert.Equal(4, read.Slots.Count);
    }

    [Fact]
    public async Task The_same_quarter_hour_cannot_be_booked_twice_on_one_resource()
    {
        using var first = fixture.CreateScope();
        var db = first.ServiceProvider.GetRequiredService<SlotBookDbContext>();

        var resourceId = await CreateResourceAsync(db, "Sala Kolumnowa");

        db.Reservations.Add(Reservation.For(resourceId, new TimeSlot(At(10), At(11))));
        await db.SaveChangesAsync();

        // A second scope, so a second DbContext. One context would refuse the write on its own:
        // the key is meaningful, so the identity map already holds those rows and the insert
        // would never be sent. Two requests never share a context, and neither may this test.
        using var second = fixture.CreateScope();
        var other = second.ServiceProvider.GetRequiredService<SlotBookDbContext>();

        other.Reservations.Add(Reservation.For(resourceId, new TimeSlot(At(10, 30), At(11, 30))));

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());

        // 2627 is a primary key violation, 2601 a unique index one. The rule is carried by the
        // composite primary key here, so it answers 2627 - and the endpoint will map it with
        // the same predicate that already turns a duplicate resource name into 409.
        var sql = Assert.IsType<SqlException>(error.InnerException);
        Assert.Equal(2627, sql.Number);
    }

    [Fact]
    public async Task A_reservation_for_a_resource_that_does_not_exist_is_refused()
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SlotBookDbContext>();

        db.Reservations.Add(Reservation.For(resourceId: 999999, new TimeSlot(At(10), At(11))));

        // 547 is a foreign key violation. Reservation has no navigation to Resource, and EF Core
        // reads relationships from navigations rather than from column names, so without an
        // explicit mapping there would be no constraint here and this insert would succeed.
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        var sql = Assert.IsType<SqlException>(error.InnerException);
        Assert.Equal(547, sql.Number);
    }

    private static async Task<int> CreateResourceAsync(SlotBookDbContext db, string name)
    {
        var resource = new Resource { Name = name, Kind = ResourceKind.Room };
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        return resource.Id;
    }
}
