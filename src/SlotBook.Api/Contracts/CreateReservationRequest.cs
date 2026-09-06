using System.ComponentModel.DataAnnotations;

namespace SlotBook.Api.Contracts;

// Not a positional record, for the reason spelled out on CreateResourceRequest: positional
// binding runs through the constructor, where an omitted field becomes default(T) instead of a
// rejected payload. Here that would turn a missing startsAt into DateTimeOffset.MinValue.
public sealed record CreateReservationRequest
{
    // Identity columns start at 1, so anything below it names a row that can never exist. The
    // check costs nothing, answers before the database is asked, and publishes minimum 1 in the
    // OpenAPI document. An id that is positive but unknown is a different answer, and it comes
    // from reading the resource in the handler.
    [Range(1, int.MaxValue)]
    public required int ResourceId { get; init; }

    // No attribute guards the order of these two, or their alignment to the booking grid.
    // DataAnnotations validate one member at a time, and both rules span the pair, so the
    // handler asks about them and answers 400 in the same shape the filter would.
    public required DateTimeOffset StartsAt { get; init; }

    public required DateTimeOffset EndsAt { get; init; }
}
