namespace SlotBook.Core.Tests;

public sealed class ReservationTests
{
    [Fact]
    public void A_booked_hour_produces_one_row_per_quarter_hour()
    {
        var reservation = Reservation.For(resourceId: 7, new TimeSlot(At(10), At(11)));

        // The rows are what the unique key will see, so the resource has to travel down into
        // each one. It is duplicated from the parent on purpose: an index cannot reach through
        // a foreign key to read it.
        Assert.Equal(4, reservation.Slots.Count);
        Assert.All(reservation.Slots, slot => Assert.Equal(7, slot.ResourceId));
    }

    [Fact]
    public void The_rows_carry_the_indexes_the_grid_gives()
    {
        var period = new TimeSlot(At(10), At(11));

        var reservation = Reservation.For(resourceId: 7, period);

        // Compared against BookingGrid rather than against literal numbers. The risk being
        // ruled out is a second copy of the same arithmetic that later drifts from the first.
        Assert.Equal(BookingGrid.IndexesFor(period), reservation.Slots.Select(row => row.SlotIndex));
    }

    [Fact]
    public void A_reservation_keeps_the_period_it_was_booked_for()
    {
        var period = new TimeSlot(At(10), At(11));

        var reservation = Reservation.For(resourceId: 7, period);

        // Cancelling deletes the rows, so from then on the parent is the only record of when
        // the booking was for. An implementation that kept only the expanded rows would lose it.
        Assert.Equal(period, reservation.Period);
    }

    // The date is arbitrary; only the time of day carries meaning. Offset zero keeps the fixture
    // out of the machine's time zone.
    private static DateTimeOffset At(int hour, int minute = 0) =>
        new(2026, 9, 4, hour, minute, 0, TimeSpan.Zero);
}
