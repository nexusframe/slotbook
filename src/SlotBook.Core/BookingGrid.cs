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

    // The question a caller asks before building a slot it means to store. IndexOf asserts the
    // same thing, because past that point an unaligned instant is a mistake in the code rather
    // than bad input, and the two need different answers.
    public static bool IsAligned(DateTimeOffset instant) => instant.UtcTicks % QuantumTicks == 0;

    // UtcTicks rather than the local clock: the same instant has to land on the same quantum
    // whatever offset it arrived in. The origin is arbitrary as long as it is fixed, so ticks
    // count from year one and no epoch constant is needed.
    private static int IndexOf(DateTimeOffset instant, string paramName)
    {
        if (!IsAligned(instant))
        {
            throw new ArgumentException(
                $"A booking has to fall on a {QuantumMinutes} minute boundary, and {instant:O} does not.",
                paramName);
        }

        return (int)(instant.UtcTicks / QuantumTicks);
    }
}
