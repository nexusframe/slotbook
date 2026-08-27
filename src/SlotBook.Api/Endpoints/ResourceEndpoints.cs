using Microsoft.EntityFrameworkCore;
using SlotBook.Api.Contracts;
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

        return group;
    }
}
