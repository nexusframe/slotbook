using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SlotBook.Api.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public sealed class ResourceEndpointsTests(SlotBookApiFixture fixture)
{
    [Fact]
    public async Task Get_resources_returns_ok_with_a_json_array()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/resources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Not "is empty": every class in this collection writes to the same database, and the
        // order they run in is not guaranteed.
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
    }

    [Fact]
    public async Task Post_resources_returns_created_and_location_serves_the_new_resource()
    {
        var client = fixture.CreateClient();

        // An anonymous object, not the request DTO: the test states the wire format the API
        // promises, and stays honest if the DTO is later renamed or reshaped.
        var response = await client.PostAsJsonAsync(
            "/resources",
            new { name = "Sala Sejmowa", kind = "Room" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Read as raw JSON rather than ResourceResponse. Deserialising into the record would
        // hide the one thing worth pinning here: that kind travels as "Room" and not as 0.
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Sala Sejmowa", created.GetProperty("name").GetString());
        Assert.Equal("Room", created.GetProperty("kind").GetString());
        Assert.True(created.GetProperty("isActive").GetBoolean());

        // The client should not have to know how the API builds its URLs, so the test does not
        // assemble "/resources/" + id either. It follows the address the server handed back.
        var location = response.Headers.Location;
        Assert.NotNull(location);

        var followed = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, followed.StatusCode);

        var fetched = await followed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(created.GetProperty("id").GetInt32(), fetched.GetProperty("id").GetInt32());
        Assert.Equal("Sala Sejmowa", fetched.GetProperty("name").GetString());
        Assert.Equal("Room", fetched.GetProperty("kind").GetString());
    }
}
