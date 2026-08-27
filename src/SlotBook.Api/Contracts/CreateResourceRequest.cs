using SlotBook.Core;

namespace SlotBook.Api.Contracts;

// Deliberately not a positional record. System.Text.Json binds those through the constructor,
// and a constructor parameter the payload omits is filled with default(T) - an absent "kind"
// would quietly become Room. A required init-only property turns the same omission into a
// deserialisation failure, which the framework reports as 400.
public sealed record CreateResourceRequest
{
    public required string Name { get; init; }

    public required ResourceKind Kind { get; init; }
}
