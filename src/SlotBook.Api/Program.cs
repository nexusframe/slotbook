using Microsoft.EntityFrameworkCore;
using SlotBook.Api.Endpoints;
using SlotBook.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SlotBook")
    ?? throw new InvalidOperationException("Connection string 'SlotBook' is not configured.");

builder.Services.AddDbContext<SlotBookDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

app.MapResourceEndpoints();

app.Run();

// Top-level statements compile into an internal Program class. WebApplicationFactory<T>
// needs the entry point type to be visible from the test assembly.
public partial class Program;
