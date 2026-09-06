using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SlotBook.Api.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public sealed class ReservationEndpointsTests(SlotBookApiFixture fixture)
{
    [Fact]
    public async Task Post_reservations_returns_created_and_location_serves_the_new_reservation()
    {
        var client = fixture.CreateClient();
        var resourceId = await CreateResourceAsync(client, "Sala Rycerska");

        // Instants as strings, the way a client sends them. The offset is part of the value, so
        // the API is never asked to guess which clock the caller meant.
        var response = await client.PostAsJsonAsync(
            "/reservations",
            new
            {
                resourceId,
                startsAt = "2026-09-07T10:00:00+00:00",
                endsAt = "2026-09-07T11:00:00+00:00",
            });

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

    private static DateTimeOffset At(int hour) =>
        new(2026, 9, 7, hour, 0, 0, TimeSpan.Zero);

    private static async Task<int> CreateResourceAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/resources", new { name, kind = "Room" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();

        return created.GetProperty("id").GetInt32();
    }
}
