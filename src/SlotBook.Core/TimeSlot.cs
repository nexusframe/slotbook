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

    public bool Overlaps(TimeSlot other) => throw new NotImplementedException();
}