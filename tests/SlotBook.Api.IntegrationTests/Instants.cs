namespace SlotBook.Api.IntegrationTests;

// A copy of the helper in SlotBook.Core.Tests, on purpose. A shared project for four lines
// would cost more than it saves, and the two suites are free to disagree about their date.
internal static class Instants
{
    // The date is arbitrary; only the time of day carries meaning. Every test books its own
    // resource, so tests may share hours without ever sharing a quarter hour.
    public static DateTimeOffset At(int hour, int minute = 0) =>
        new(2026, 9, 8, hour, minute, 0, TimeSpan.Zero);
}
