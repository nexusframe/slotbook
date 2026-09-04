namespace SlotBook.Core.Tests;

public sealed class TimeSlotTests
{
    [Fact]
    public void Adjacent_slots_do_not_overlap()
    {
        var morning = new TimeSlot(At(10), At(11));
        var midday = new TimeSlot(At(11), At(12));

        // The case that decides the model. Under a closed interval both slots contain 11:00,
        // so any overlap test reports a conflict and no room can be booked back to back. Under
        // a half-open one the end is the moment the room is free again.
        Assert.False(morning.Overlaps(midday));
    }

    [Fact]
    public void Slots_that_share_an_hour_overlap()
    {
        var morning = new TimeSlot(At(10), At(12));
        var late = new TimeSlot(At(11), At(13));

        // The pair carries more than either case alone. An Overlaps that always answers false
        // satisfies the adjacency test by itself, and no constant satisfies both.
        Assert.True(morning.Overlaps(late));
    }


    // The date is arbitrary; only the hours carry meaning. Offset zero keeps the fixture out of
    // the machine's time zone.
    private static DateTimeOffset At(int hour) => new(2026, 9, 4, hour, 0, 0, TimeSpan.Zero);
}
