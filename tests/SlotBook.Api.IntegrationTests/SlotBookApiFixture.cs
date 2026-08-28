using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SlotBook.Infrastructure;
using Testcontainers.MsSql;

namespace SlotBook.Api.IntegrationTests;

// One SQL Server container and one running API for the whole test collection. Starting the
// container costs seconds; starting it per test class would cost minutes.
public sealed class SlotBookApiFixture : IAsyncLifetime
{
    // Same image as docker-compose.yml. Deliberately no WithDatabase(): the module would
    // create the database itself, and EF Core only turns READ_COMMITTED_SNAPSHOT on for a
    // database it creates. Letting EF create it keeps tests and development on one isolation
    // level.
    private readonly MsSqlContainer _sqlServer =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private WebApplicationFactory<Program>? _factory;

    public HttpClient CreateClient() =>
        (_factory ?? throw new InvalidOperationException("Fixture was not initialised."))
            .CreateClient();

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();

        // Program.cs reads the connection string from IConfiguration, so the test host only
        // has to supply a different value — the DbContext registration itself is untouched.
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:SlotBook", BuildConnectionString());

            // EF Core logs every statement it runs at Information, which is worth having when
            // a query is the suspect and noise when it is not. Warnings and errors still come
            // through, and a failing assertion reports itself either way.
            builder.UseSetting("Logging:LogLevel:Default", "Warning");
        });

        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SlotBookDbContext>().Database;
        await database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _sqlServer.DisposeAsync();
    }

    // The container hands out a connection string pointing at master.
    private string BuildConnectionString() =>
        new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "SlotBook",
        }.ConnectionString;
}
