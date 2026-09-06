using SlotBook.Core;

namespace SlotBook.Api.Contracts;

// The period, not the rows it expands into. Quarter-hour indexes are how the booking is stored
// and how the database refuses a second one; publishing them would put the fifteen minute grid
// in the contract, where changing it would cost a new version of the API.
public sealed record ReservationResponse(
    int Id,
    int ResourceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    ReservationStatus Status);
