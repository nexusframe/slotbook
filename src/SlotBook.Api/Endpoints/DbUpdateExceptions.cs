using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SlotBook.Api.Endpoints;

internal static class DbUpdateExceptions
{
    // 2627 is a violated UNIQUE constraint, 2601 a violated unique index. Which one arrives
    // depends only on how the rule was declared - resource names carry a unique index, booked
    // quarter hours a composite primary key - so a caller has no reason to tell them apart.
    //
    // The endpoints ask this question themselves rather than leaving it to middleware: both
    // numbers say "some unique key", not which one. A global translator would have to parse the
    // message text to say more, and those are localised in the language the server was
    // installed with.
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
