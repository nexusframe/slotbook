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
}
