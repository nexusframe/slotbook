namespace SlotBook.Core;

public sealed class ReservationSlot
{
    public int ReservationId { get; set; }

    public int ResourceId { get; set; }

    public int SlotIndex { get; set; }
}