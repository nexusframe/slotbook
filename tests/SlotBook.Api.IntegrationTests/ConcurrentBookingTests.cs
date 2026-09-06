using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SlotBook.Infrastructure;

using static SlotBook.Api.IntegrationTests.Instants;

namespace SlotBook.Api.IntegrationTests;

// The rule the project exists to prove: no two reservations overlap on one resource, and the
// database is what refuses the second one. Every request here goes through HTTP, so each gets
// its own DI scope and its own DbContext. A test sharing one context would pass on the change
// tracker alone - the key (ResourceId, SlotIndex) is meaningful, so the identity map rejects
// the duplicate before any INSERT leaves the process.
[Collection(ApiCollectionDefinition.Name)]
public sealed class ConcurrentBookingTests(SlotBookApiFixture fixture)
{
    private const int Contenders = 8;

    [Fact]
    public async Task Post_reservations_gives_the_period_to_exactly_one_of_eight_simultaneous_requests()
    {
        var client = fixture.CreateClient();
        var resourceId = await CreateResourceAsync(client, "Sala Kongresowa");

        // A gate, because building the tasks does not start them together: an async method runs
        // on the calling thread until its first await, so without this the first request would
        // be committed before the eighth was built. RunContinuationsAsynchronously carries as
        // much weight as the gate itself - by default SetResult runs every waiting continuation
        // inline, one after another, on the releasing thread, which is a queue by another name.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var attempts = Enumerable
            .Range(0, Contenders)
            .Select(async _ =>
            {
                await gate.Task;

                return await BookAsync(client, resourceId, At(10), At(11));
            })
            .ToArray();

        gate.SetResult();

        var responses = await Task.WhenAll(attempts);

        var created = responses.Count(response => response.StatusCode == HttpStatusCode.Created);
        var conflicted = responses.Count(response => response.StatusCode == HttpStatusCode.Conflict);

        // The losers are counted rather than asserted to be "not created". A 500 from a
        // deadlock, or a 400 from a handler that lost its footing, is also not a 201, and it
        // would satisfy the weaker assertion while meaning the mechanism came apart.
        Assert.Equal(1, created);
        Assert.Equal(Contenders - 1, conflicted);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SlotBookDbContext>();

        // What the answers claimed, against what the database holds. A handler that reported a
        // conflict and wrote its rows anyway would pass on the status codes alone. Four quarter
        // hours, because one row lands per quantum and the hour is the winner's alone.
        Assert.Equal(1, await db.Reservations.CountAsync(r => r.ResourceId == resourceId));
        Assert.Equal(4, await db.ReservationSlots.CountAsync(slot => slot.ResourceId == resourceId));
    }

    // Round-trip format, so the offset travels with the instant and the API is never asked to
    // guess which clock the caller meant.
    private static Task<HttpResponseMessage> BookAsync(
        HttpClient client,
        int resourceId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt) =>
        client.PostAsJsonAsync(
            "/reservations",
            new
            {
                resourceId,
                startsAt = startsAt.ToString("O", CultureInfo.InvariantCulture),
                endsAt = endsAt.ToString("O", CultureInfo.InvariantCulture),
            });

    private static async Task<int> CreateResourceAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/resources", new { name, kind = "Room" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();

        return created.GetProperty("id").GetInt32();
    }
}
