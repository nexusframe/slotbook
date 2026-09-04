namespace SlotBook.Core;

public sealed class Reservation
{
    public int Id { get; set; }

    public int ResourceId { get; set; }

    public TimeSlot Period { get; set; }

    public ReservationStatus Status { get; set; }

    public List<ReservationSlot> Slots { get; } = [];

    public static Reservation For(int resourceId, TimeSlot period) =>
        throw new NotImplementedException();
}