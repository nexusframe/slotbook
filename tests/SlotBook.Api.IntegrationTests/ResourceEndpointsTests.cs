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

    [Fact]
    public async Task Post_resources_returns_conflict_when_the_name_is_already_taken()
    {
        var client = fixture.CreateClient();

        var first = await client.PostAsJsonAsync(
            "/resources",
            new { name = "Biurko 12", kind = "Desk" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync(
            "/resources",
            new { name = "Biurko 12", kind = "Room" });

        // The endpoint is not expected to look the name up first. It inserts and lets the
        // unique index answer, which is the same argument the overlap rule will make at a
        // larger scale: a check that precedes the write has a gap another writer fits into.
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Post_resources_returns_conflict_for_a_name_differing_only_by_case_and_trailing_space()
    {
        var client = fixture.CreateClient();

        var first = await client.PostAsJsonAsync(
            "/resources",
            new { name = "Sala Konferencyjna", kind = "Room" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Two distinct strings in C#. Under SQL_Latin1_General_CP1_CI_AS they are one index
        // key: the collation is case-insensitive and string comparison ignores trailing
        // blanks. Uniqueness here means what the database means by it, not what Equals does.
        var second = await client.PostAsJsonAsync(
            "/resources",
            new { name = "sala konferencyjna ", kind = "Room" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Put_resources_replaces_every_field_and_answers_no_content()
    {
        var client = fixture.CreateClient();

        var created = await client.PostAsJsonAsync(
            "/resources",
            new { name = "Sala Alfa", kind = "Room" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var location = created.Headers.Location;
        Assert.NotNull(location);

        // All three fields at once, including isActive: PUT replaces the representation rather
        // than patching it, and deactivation travels this way instead of through an endpoint
        // of its own. Sending isActive true again is what reactivates a resource.
        var updated = await client.PutAsJsonAsync(
            location,
            new { name = "Sala Beta", kind = "Desk", isActive = false });

        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);

        // 204 carries no body, so the only way to see whether anything happened is to read the
        // resource back. A test that stopped at the status code would pass against an endpoint
        // that returned 204 and wrote nothing.
        var fetched = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

        var body = await fetched.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Sala Beta", body.GetProperty("name").GetString());
        Assert.Equal("Desk", body.GetProperty("kind").GetString());
        Assert.False(body.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Put_resources_returns_not_found_for_an_unknown_id()
    {
        var client = fixture.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/resources/999999",
            new { name = "Sala Widmo", kind = "Room", isActive = true });

        // Not an insert. PUT to an address the server never handed out is a mistake by the
        // client, and inventing the resource there would let it choose its own identifiers.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_resources_returns_conflict_when_the_name_belongs_to_another_resource()
    {
        var client = fixture.CreateClient();

        var occupant = await client.PostAsJsonAsync(
            "/resources",
            new { name = "Sala Gamma", kind = "Room" });

        Assert.Equal(HttpStatusCode.Created, occupant.StatusCode);

        var subject = await client.PostAsJsonAsync(
            "/resources",
            new { name = "Sala Delta", kind = "Room" });

        Assert.Equal(HttpStatusCode.Created, subject.StatusCode);

        var location = subject.Headers.Location;
        Assert.NotNull(location);

        // The same unique index guards the UPDATE, and for the same reason it guards the
        // INSERT: the endpoint is not expected to look the name up first.
        var response = await client.PutAsJsonAsync(
            location,
            new { name = "Sala Gamma", kind = "Room", isActive = true });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_resources_returns_no_content_when_the_name_is_unchanged()
    {
        var client = fixture.CreateClient();

        var created = await client.PostAsJsonAsync(
            "/resources",
            new { name = "Sala Epsilon", kind = "Room" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var location = created.Headers.Location;
        Assert.NotNull(location);

        // The row already holds this name, so an implementation that checks for a duplicate
        // without excluding the row being updated answers 409 here. The unique index does not
        // make that mistake: UPDATE leaves the key where it already was.
        var response = await client.PutAsJsonAsync(
            location,
            new { name = "Sala Epsilon", kind = "Desk", isActive = true });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
