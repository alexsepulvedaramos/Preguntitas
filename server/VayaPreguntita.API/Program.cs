using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Validators;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// 1. Service Registration
// ====================================================================

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateQuestionDtoValidator>();

// Configure the database with PostgreSQL
var connectionString =
    builder.Configuration.GetConnectionString("Supabase")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Swagger/OpenAPI setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register AutoMapper using the new configuration action syntax
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

var app = builder.Build();

// ====================================================================
// 2. HTTP Request Pipeline (Middleware)
// ====================================================================

if (app.Environment.IsDevelopment())
{
    // Enable the middleware to serve generated Swagger as a JSON endpoint
    app.UseSwagger();

    // Enable the middleware to serve Swagger UI (the visual interface)
    // This is what was missing!
    app.UseSwaggerUI();
}

// Comment this out temporarily if you have issues with local certificates
// app.UseHttpsRedirection();

app.UseAuthorization();

// Map controller routes
app.MapControllers();

app.Run();
