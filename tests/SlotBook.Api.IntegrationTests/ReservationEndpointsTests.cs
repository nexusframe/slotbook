using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;

using static SlotBook.Api.IntegrationTests.Instants;

namespace SlotBook.Api.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public sealed class ReservationEndpointsTests(SlotBookApiFixture fixture)
{
    [Fact]
    public async Task Post_reservations_returns_created_and_location_serves_the_new_reservation()
    {
        var client = fixture.CreateClient();
        var resourceId = await CreateResourceAsync(client, "Sala Rycerska");

        var response = await BookAsync(client, resourceId, At(10), At(11));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(resourceId, created.GetProperty("resourceId").GetInt32());
        Assert.Equal("Confirmed", created.GetProperty("status").GetString());

        // The quarter hours a booking occupies are how it is stored, not what it is. Publishing
        // them would freeze the fifteen minute grid into the contract, and the grid is meant to
        // stay changeable without a new API version.
        Assert.False(created.TryGetProperty("slots", out _));

        var location = response.Headers.Location;
        Assert.NotNull(location);

        var followed = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, followed.StatusCode);

        var fetched = await followed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(created.GetProperty("id").GetInt32(), fetched.GetProperty("id").GetInt32());

        // Read as DateTimeOffset rather than as text: the API is free to write the instant in
        // any equivalent offset, and a client comparing strings would be the one at fault.
        Assert.Equal(At(10), fetched.GetProperty("startsAt").GetDateTimeOffset());
        Assert.Equal(At(11), fetched.GetProperty("endsAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Post_reservations_returns_conflict_when_the_quarter_hours_are_taken()
    {
        var client = fixture.CreateClient();
        var resourceId = await CreateResourceAsync(client, "Sala Balowa");

        var first = await BookAsync(client, resourceId, At(10), At(11));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Not the same period, only an overlapping one, and the two share the quarter hours from
        // 10:30. Nothing in the handler compares periods: the second insert repeats a row the
        // composite key already holds, and the answer comes back from the database.
        var second = await BookAsync(client, resourceId, At(10, 30), At(11, 30));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Post_reservations_books_a_period_starting_when_another_one_ends()
    {
        var client = fixture.CreateClient();
        var resourceId = await CreateResourceAsync(client, "Sala Lustrzana");

        var morning = await BookAsync(client, resourceId, At(10), At(11));
        Assert.Equal(HttpStatusCode.Created, morning.StatusCode);

        // The half-open interval, seen from outside. A room is free again at the instant its
        // booking ends, so 11:00 belongs to the second period alone. Had the expansion emitted
        // a row for the end instant too, this request would be a conflict and no resource in
        // the system could ever be booked back to back.
        var midday = await BookAsync(client, resourceId, At(11), At(12));

        Assert.Equal(HttpStatusCode.Created, midday.StatusCode);
    }

    [Fact]
    public async Task Post_reservations_returns_bad_request_for_a_period_off_the_grid()
    {
        var client = fixture.CreateClient();
        var resourceId = await CreateResourceAsync(client, "Sala Marmurowa");

        // 10:05 sits inside a quarter hour rather than on its edge. Rounding it would hand back
        // time nobody asked for or block a neighbour, so the request is refused instead.
        var response = await BookAsync(client, resourceId, At(10, 5), At(11));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        // Keyed by the CLR member name, the same as the rejections the validation filter writes.
        // One status code with two spellings of a field is a contract a client cannot parse.
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors");

        Assert.True(
            errors.TryGetProperty("StartsAt", out _),
            $"Expected an entry for the StartsAt member, got: {errors}");
    }

    [Fact]
    public async Task Post_reservations_returns_bad_request_when_the_end_does_not_follow_the_start()
    {
        var client = fixture.CreateClient();
        var resourceId = await CreateResourceAsync(client, "Sala Senatorska");

        // Both ends are on the grid, so this is a fault of the pair rather than of either value.
        // No DataAnnotation can state it, because validation sees one member at a time.
        var response = await BookAsync(client, resourceId, At(11), At(10));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("EndsAt", out _));
    }

    [Fact]
    public async Task Post_reservations_returns_bad_request_for_a_resource_that_does_not_exist()
    {
        var client = fixture.CreateClient();

        // Not 404: the address /reservations exists, and the resource is named in the body
        // rather than addressed by the URL. A nested route would owe the other answer.
        var response = await BookAsync(client, resourceId: 999999, At(10), At(11));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("ResourceId", out _));
    }

    [Fact]
    public async Task Post_reservations_returns_bad_request_for_a_deactivated_resource()
    {
        var client = fixture.CreateClient();
        var resourceId = await CreateResourceAsync(client, "Sala Zamknieta");

        var deleted = await client.DeleteAsync($"/resources/{resourceId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        // The row is still there, so the foreign key has no objection. This one answer comes
        // from a read, and it is allowed to: nothing reactivates the resource in the gap, and
        // booking a room switched off a moment ago harms nobody.
        var response = await BookAsync(client, resourceId, At(10), At(11));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
