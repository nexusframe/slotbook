namespace SlotBook.Api.IntegrationTests;

// Test classes carrying [Collection(ApiCollectionDefinition.Name)] share one SlotBookApiFixture and run
// one after another. Sharing a database is the price of sharing a container.
[CollectionDefinition(ApiCollectionDefinition.Name)]
public sealed class ApiCollectionDefinition : ICollectionFixture<SlotBookApiFixture>
{
    public const string Name = "SlotBook API";
}
