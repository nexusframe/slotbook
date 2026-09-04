namespace SlotBook.Core;

public static class BookingGrid
{
    public const int QuantumMinutes = 15;

    private const long QuantumTicks = TimeSpan.TicksPerMinute * QuantumMinutes;

    public static IReadOnlyList<int> IndexesFor(TimeSlot slot)
    {
        var first = IndexOf(slot.Start, nameof(slot));
        var last = IndexOf(slot.End, nameof(slot));

        return Enumerable.Range(first, last - first).ToArray();
    }

    // UtcTicks rather than the local clock: the same instant has to land on the same quantum
    // whatever offset it arrived in. The origin is arbitrary as long as it is fixed, so ticks
    // count from year one and no epoch constant is needed.
    private static int IndexOf(DateTimeOffset instant, string paramName)
    {
        if (instant.UtcTicks % QuantumTicks != 0)
        {
            throw new ArgumentException(
                $"A booking has to fall on a {QuantumMinutes} minute boundary, and {instant:O} does not.",
                paramName);
        }

        return (int)(instant.UtcTicks / QuantumTicks);
    }
}
