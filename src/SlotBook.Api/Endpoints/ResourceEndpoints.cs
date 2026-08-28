using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SlotBook.Api.Contracts;
using SlotBook.Core;
using SlotBook.Infrastructure;

namespace SlotBook.Api.Endpoints;

internal static class ResourceEndpoints
{
    public static RouteGroupBuilder MapResourceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/resources").WithTags("Resources");

        // Projecting straight into the response type keeps the query to the four columns it
        // needs, and the result is not an entity, so nothing enters the change tracker.
        group.MapGet("/", async (SlotBookDbContext db, CancellationToken cancellationToken) =>
            await db.Resources
                .Select(resource => new ResourceResponse(
                    resource.Id, resource.Name, resource.Kind, resource.IsActive))
                .ToListAsync(cancellationToken))
            .WithSummary("Lists every resource, active or not.");

        group.MapGet("/{id:int}", async Task<Results<Ok<ResourceResponse>, NotFound>> (
            int id,
            SlotBookDbContext db,
            CancellationToken cancellationToken) =>
        {
            var found = await db.Resources
                .Where(resource => resource.Id == id)
                .Select(resource => new ResourceResponse(
                    resource.Id, resource.Name, resource.Kind, resource.IsActive))
                .FirstOrDefaultAsync(cancellationToken);

            return found is null ? TypedResults.NotFound() : TypedResults.Ok(found);
        })
            .WithSummary("Reads one resource by id.");

        group.MapPost("/", async Task<Results<Created<ResourceResponse>, Conflict>> (
            CreateResourceRequest request,
            SlotBookDbContext db,
            CancellationToken cancellationToken) =>
        {
            var resource = new Resource { Name = request.Name, Kind = request.Kind };

            db.Resources.Add(resource);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            // No lookup before the insert. A query that asks whether the name is free answers
            // for the moment it ran, and another writer fits into the gap between that answer
            // and this INSERT. The unique index answers for the moment the row lands.
            catch (DbUpdateException e) when (IsUniqueViolation(e))
            {
                return TypedResults.Conflict();
            }

            // The identity value is only known after SaveChangesAsync, which is what makes
            // Location something the server can hand back and the client cannot predict.
            return TypedResults.Created(
                $"/resources/{resource.Id}",
                new ResourceResponse(
                    resource.Id, resource.Name, resource.Kind, resource.IsActive));
        })
            .WithSummary("Creates a resource.");

        group.MapPut("/{id:int}", async Task<Results<NoContent, NotFound, Conflict>> (
            int id,
            UpdateResourceRequest request,
            SlotBookDbContext db,
            CancellationToken cancellationToken) =>
        {
            // The tracked entity, not a projection. SaveChangesAsync writes what the change
            // tracker sees differ from the values it loaded, so the object has to be the one
            // the context is watching. FindAsync is the primary-key lookup: it returns an
            // already-tracked instance without a round trip, and queries only otherwise.
            var resource = await db.Resources.FindAsync([id], cancellationToken);

            if (resource is null)
            {
                return TypedResults.NotFound();
            }

            resource.Name = request.Name;
            resource.Kind = request.Kind;
            resource.IsActive = request.IsActive;

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            // The same index guards the UPDATE as guards the INSERT. Nothing here excludes the
            // row being updated from the check, because there is no check: writing a key back
            // where it already sat is not a duplicate, and the index knows that unprompted.
            catch (DbUpdateException e) when (IsUniqueViolation(e))
            {
                return TypedResults.Conflict();
            }

            return TypedResults.NoContent();
        })
            .WithSummary("Replaces a resource.");

        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (
            int id,
            SlotBookDbContext db,
            CancellationToken cancellationToken) =>
        {
            // Deactivation, not removal: reservations will point at resources, so deleting the
            // row would either fail on the foreign key or orphan the history.
            //
            // One statement, and the count it returns answers whether the resource existed -
            // no SELECT first, and no gap between asking and writing. Idempotence comes out of
            // the same count: setting IsActive false on a row that already holds false still
            // reports one row updated, so a repeated DELETE answers 204 with no code for it.
            var affected = await db.Resources
                .Where(resource => resource.Id == id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(resource => resource.IsActive, false),
                    cancellationToken);

            return affected == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
        })
            .WithSummary("Deactivates a resource.");

        return group;
    }

    // 2627 is a violated UNIQUE constraint, 2601 a violated unique index. Which one arrives
    // depends only on how the rule was declared, so a caller has no reason to tell them apart.
    //
    // The endpoints ask this question themselves rather than leaving it to middleware: both
    // numbers say "some unique index", not which one, and this table has exactly one. A global
    // translator would have to parse the message text to say more, and those are localised in
    // the language the server was installed with.
    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
