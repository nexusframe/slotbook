using SlotBook.Core;

namespace SlotBook.Api.Contracts;

public sealed record ResourceResponse(int Id, string Name, ResourceKind Kind, bool IsActive);
