using SlotBook.Core;

namespace SlotBook.Api.Contracts;

// Separate from CreateResourceRequest because it carries IsActive: PUT replaces the whole
// representation, and deactivating a resource travels this way rather than through an endpoint
// of its own. Required for the same reason it is required there - an omitted bool would arrive
// as false and silently deactivate whatever it touched.
public sealed record UpdateResourceRequest
{
    public required string Name { get; init; }

    public required ResourceKind Kind { get; init; }

    public required bool IsActive { get; init; }
}
