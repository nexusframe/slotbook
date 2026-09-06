namespace SlotBook.Core.Tests;

internal static class Instants
{
    // The date is arbitrary; only the time of day carries meaning anywhere in these tests.
    // Offset zero keeps the fixtures out of the machine's time zone, and a test that is about
    // the offset says so by building its own value instead.
    public static DateTimeOffset At(int hour, int minute = 0) =>
        new(2026, 9, 4, hour, minute, 0, TimeSpan.Zero);
}
