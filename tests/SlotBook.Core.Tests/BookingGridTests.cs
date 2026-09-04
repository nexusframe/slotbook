namespace SlotBook.Core.Tests;

public sealed class BookingGridTests
{
    [Fact]
    public void An_hour_covers_four_quarter_hours()
    {
        var indexes = BookingGrid.IndexesFor(new TimeSlot(At(10), At(11)));

        // The quantum, stated as behaviour rather than as a constant a reader has to go and
        // look up. Four rows per booked hour is also the number the unique index has to carry.
        Assert.Equal(4, indexes.Count);
    }

    [Fact]
    public void Adjacent_slots_cover_no_common_quarter_hour()
    {
        var morning = BookingGrid.IndexesFor(new TimeSlot(At(10), At(11)));
        var midday = BookingGrid.IndexesFor(new TimeSlot(At(11), At(12)));

        // Where the off-by-one lives. The instant a slot ends opens the next one, so the quarter
        // hour beginning at 11:00 belongs to midday alone. Emitting it for both would make the
        // unique index refuse a back-to-back booking that TimeSlot.Overlaps already allows.
        Assert.Empty(morning.Intersect(midday));
    }

    [Fact]
    public void The_same_instant_in_two_offsets_covers_the_same_quarter_hour()
    {
        var warsaw = new TimeSlot(
            new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 9, 4, 12, 15, 0, TimeSpan.FromHours(2)));

        var utc = new TimeSlot(At(10), At(10, 15));

        // Both describe 10:00 UTC. Indexing off the wall clock instead of the instant would put
        // them in different quarter hours, and two clients in different offsets could then book
        // one room for one time without either write colliding with the other.
        Assert.Equal(BookingGrid.IndexesFor(utc), BookingGrid.IndexesFor(warsaw));
    }

    [Fact]
    public void A_slot_that_does_not_start_on_the_grid_is_rejected()
    {
        // 10:05 falls inside a quarter hour rather than on its edge. Rounding it would either
        // hand the caller more time than was asked for or block a neighbour who did nothing
        // wrong, and both are decisions the caller should be making instead.
        Assert.Throws<ArgumentException>(
            () => { _ = BookingGrid.IndexesFor(new TimeSlot(At(10, 5), At(11))); });
    }

    // The date is arbitrary; only the time of day carries meaning. Offset zero keeps the fixture
    // out of the machine's time zone, except where a test is about the offset itself.
    private static DateTimeOffset At(int hour, int minute = 0) =>
        new(2026, 9, 4, hour, minute, 0, TimeSpan.Zero);
}
