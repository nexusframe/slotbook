namespace SlotBook.Core;

public readonly record struct TimeSlot
{
    public TimeSlot(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    // Half-open [Start, End): both comparisons are strict, so a slot ending exactly when
    // another begins is not a conflict.
    public bool Overlaps(TimeSlot other)
    {
        return Start < other.End && other.Start < End;
    }
}