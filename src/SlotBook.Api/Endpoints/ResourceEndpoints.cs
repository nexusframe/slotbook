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
            //
            // 2627 is a violated UNIQUE constraint, 2601 a violated unique index; which one
            // arrives depends on how the rule was declared, so both mean the same thing here.
            catch (DbUpdateException e)
                when (e.InnerException is SqlException { Number: 2601 or 2627 })
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

        return group;
    }
}
