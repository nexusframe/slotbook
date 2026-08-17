using Microsoft.EntityFrameworkCore;
using SlotBook.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SlotBook")
    ?? throw new InvalidOperationException("Connection string 'SlotBook' is not configured.");

builder.Services.AddDbContext<SlotBookDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
