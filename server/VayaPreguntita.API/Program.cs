using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// 1. Service Registration (Dependency Injection Container)
// ====================================================================

// Add support for Controllers (MVC Pattern).
// This replaces the "Minimal API" approach, allowing for better code organization.
builder.Services.AddControllers();

// Configure OpenAPI (Swagger) for API documentation.
builder.Services.AddOpenApi();

// --- Database Configuration ---
// Retrieve the connection string from configuration (appsettings.json or User Secrets).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register the Database Context (AppDbContext) with the Dependency Injection container.
// We configure it to use PostgreSQL as the database provider.
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

// ====================================================================
// 2. HTTP Request Pipeline (Middleware)
// ====================================================================

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable OpenAPI/Swagger endpoints in development for testing.
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Map incoming HTTP requests to the controller actions.
// This tells .NET to look for classes inheriting from 'ControllerBase'.
app.MapControllers();

app.Run();
