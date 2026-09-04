namespace SlotBook.Core;

public sealed class Reservation
{
    public int Id { get; set; }

    public int ResourceId { get; set; }

    public TimeSlot Period { get; set; }

    public ReservationStatus Status { get; set; }

    public List<ReservationSlot> Slots { get; } = [];

    // The only way a reservation comes into being, so the rows can never disagree with the
    // period they were expanded from. ResourceId is repeated on every row because the unique
    // key lives there and an index cannot read it through the foreign key.
    public static Reservation For(int resourceId, TimeSlot period)
    {
        var reservation = new Reservation
        {
            ResourceId = resourceId,
            Period = period,
            Status = ReservationStatus.Confirmed,
        };

        foreach (var index in BookingGrid.IndexesFor(period))
        {
            reservation.Slots.Add(new ReservationSlot
            {
                ResourceId = resourceId,
                SlotIndex = index,
            });
        }

        return reservation;
    }
}