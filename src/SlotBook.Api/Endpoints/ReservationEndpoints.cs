using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SlotBook.Api.Contracts;
using SlotBook.Core;
using SlotBook.Infrastructure;

namespace SlotBook.Api.Endpoints;

internal static class ReservationEndpoints
{
    public static RouteGroupBuilder MapReservationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/reservations").WithTags("Reservations");

        group.MapGet("/{id:int}", async Task<Results<Ok<ReservationResponse>, NotFound>> (
            int id,
            SlotBookDbContext db,
            CancellationToken cancellationToken) =>
        {
            // Period is a complex property, so its members are columns of this table and the
            // projection reaches them the same way it reaches any other. Nothing is loaded to
            // be taken apart in memory.
            var found = await db.Reservations
                .Where(reservation => reservation.Id == id)
                .Select(reservation => new ReservationResponse(
                    reservation.Id,
                    reservation.ResourceId,
                    reservation.Period.Start,
                    reservation.Period.End,
                    reservation.Status))
                .FirstOrDefaultAsync(cancellationToken);

            return found is null ? TypedResults.NotFound() : TypedResults.Ok(found);
        })
            .WithSummary("Reads one reservation by id");

        group.MapPost("/", async Task<Results<Created<ReservationResponse>, ValidationProblem, Conflict>> (
            CreateReservationRequest request,
            SlotBookDbContext db,
            CancellationToken cancellationToken) =>
        {
            // CLR member names, matching what the validation filter writes. Dictionary keys skip
            // the camelCase policy, which is set for property names only.
            var errors = new Dictionary<string, string[]>();

            if (!BookingGrid.IsAligned(request.StartsAt))
            {
                errors["StartsAt"] = [OffGrid];
            }

            // Two things can be wrong with the end, and only one is reported. Being off the grid
            // is a fault in the value itself, so it is answered first; the order of the pair is
            // only worth raising once both ends are values this API can store at all.
            if (!BookingGrid.IsAligned(request.EndsAt))
            {
                errors["EndsAt"] = [OffGrid];
            }
            else if (request.EndsAt <= request.StartsAt)
            {
                errors["EndsAt"] = ["A reservation has to end after it starts."];
            }

            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }

            // A read before the write, which the overlap rule is never allowed. The difference
            // is whether the answer can change in the gap: resources are deactivated and never
            // deleted, and booking a room that was switched off a moment ago harms nobody.
            // Whether a quarter hour is free is the opposite, and that gap is the project.
            //
            // The foreign key still stands behind this, and a test at the schema level proves
            // it. It is a backstop, not the answer a client should be given.
            var bookable = await db.Resources.AnyAsync(
                resource => resource.Id == request.ResourceId && resource.IsActive,
                cancellationToken);

            if (!bookable)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ResourceId"] = ["There is no active resource with this id."],
                });
            }

            // The constructor also refuses an end that does not follow its start, and by now it
            // cannot fire. That is the intended layering: the domain asserts the rule, the API
            // asks about it, and the caller gets a sentence rather than a 500.
            var reservation = Reservation.For(
                request.ResourceId,
                new TimeSlot(request.StartsAt, request.EndsAt));

            db.Reservations.Add(reservation);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            // No lookup for a clash before the insert. A query answers for the moment it ran,
            // and another writer fits into the gap between that answer and this INSERT. One row
            // per quarter hour under a composite key answers for the moment the rows land.
            catch (DbUpdateException e) when (e.IsUniqueViolation())
            {
                return TypedResults.Conflict();
            }

            // The identity value is only known after SaveChangesAsync, which is what makes
            // Location something the server hands back and the client cannot predict.
            return TypedResults.Created($"/reservations/{reservation.Id}", ToResponse(reservation));
        })
            .WithSummary("Books a resource for a period");

        return group;
    }

    private const string OffGrid =
        "A reservation has to start and end on a 15 minute boundary.";

    private static ReservationResponse ToResponse(Reservation reservation) =>
        new(
            reservation.Id,
            reservation.ResourceId,
            reservation.Period.Start,
            reservation.Period.End,
            reservation.Status);
}
