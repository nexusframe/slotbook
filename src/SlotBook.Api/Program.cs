using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SlotBook.Api.Endpoints;
using SlotBook.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SlotBook")
    ?? throw new InvalidOperationException("Connection string 'SlotBook' is not configured.");

builder.Services.AddDbContext<SlotBookDbContext>(options =>
    options.UseSqlServer(connectionString));

// ConfigureHttpJsonOptions is the one Minimal APIs read. AddControllers().AddJsonOptions()
// configures a different options object and would have no effect here.
//
// allowIntegerValues: false is the point of the call. Without it the enum still serialises as
// a string, but 0 remains an accepted input - two spellings of one value, one of which leaks
// the declaration order of the enum into the public contract.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false)));

// Reads the DataAnnotations on request DTOs and rejects a payload that fails them before the
// handler runs. New in .NET 10 for Minimal APIs: a source generator finds the validatable
// parameter types at build time and the registration adds one endpoint filter per endpoint.
builder.Services.AddValidation();

// One error shape for the whole API. Both kinds of rejection write their body through
// IProblemDetailsService, which nothing registers by default: without this line a validation
// failure answers application/json and an unbindable payload answers text/plain.
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapResourceEndpoints();

app.Run();

// Top-level statements compile into an internal Program class. WebApplicationFactory<T>
// needs the entry point type to be visible from the test assembly.
public partial class Program;
