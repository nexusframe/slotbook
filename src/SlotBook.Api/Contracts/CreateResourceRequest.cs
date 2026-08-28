using System.ComponentModel.DataAnnotations;
using SlotBook.Core;

namespace SlotBook.Api.Contracts;

// Deliberately not a positional record. System.Text.Json binds those through the constructor,
// and a constructor parameter the payload omits is filled with default(T) - an absent "kind"
// would quietly become Room. A required init-only property turns the same omission into a
// deserialisation failure, which the framework reports as 400.
public sealed record CreateResourceRequest
{
    // required and [Required] answer different questions and both are needed. The keyword is
    // read by the deserialiser and asks whether the key was in the payload; the attribute is
    // read by validation and asks whether the value means anything, rejecting "" and "   ".
    //
    // The length mirrors HasMaxLength(200) on the entity. Left off, an over-long name reaches
    // SQL Server and comes back as a truncation error, which is a 500 for what is a bad request.
    [Required]
    [StringLength(200)]
    public required string Name { get; init; }

    public required ResourceKind Kind { get; init; }
}
