using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VayaPreguntita.API.BackgroundServices;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Options;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Validators;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// 1. Service Registration
// ====================================================================

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "DynamicCorsPolicy",
        policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

            if (allowedOrigins != null && allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }
        }
    );
});
builder.Services.AddRateLimiter(options =>
{
    // Define a policy named "AuthLimiter"
    options.AddFixedWindowLimiter(
        "AuthLimiter",
        opt =>
        {
            opt.Window = TimeSpan.FromMinutes(5);
            opt.PermitLimit = 10;
            opt.QueueLimit = 0;
        }
    );

    // Return 429 Too Many Requests when limit is exceeded
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddHealthChecks();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateQuestionDtoValidator>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SupabaseStorageOptions>(builder.Configuration.GetSection("SupabaseStorage"));
builder.Services.Configure<VapidOptions>(builder.Configuration.GetSection("Vapid"));
builder.Services.AddHttpClient<SupabaseStorageService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGroupsService, GroupsService>();
builder.Services.AddScoped<IDailyService, DailyService>();
builder.Services.AddScoped<IQuestionsService, QuestionsService>();
builder.Services.AddScoped<IPacksService, PacksService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<DailyPreselectionService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Web Push (VAPID)
builder.Services.AddSingleton(sp =>
{
    var vapid = sp.GetRequiredService<IOptions<VapidOptions>>().Value;
    var client = new PushServiceClient();
    client.DefaultAuthentication = new VapidAuthentication(vapid.PublicKey, vapid.PrivateKey)
    {
        Subject = vapid.Subject
    };
    return client;
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrEmpty(jwtOptions?.Key))
{
    throw new InvalidOperationException("The configuration 'Jwt:Key' is not defined.");
}

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

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
// 1.b Seed the global question packs (idempotent per template)
// ====================================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await PackSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Pack seeding skipped (is the migration applied?).");
    }
}

// ====================================================================
// 2. HTTP Request Pipeline (Middleware)
// ====================================================================

if (app.Environment.IsDevelopment())
{
    // Enable the middleware to serve generated Swagger as a JSON endpoint
    app.UseSwagger();

    // Enable the middleware to serve Swagger UI (the visual interface)
    app.UseSwaggerUI();
}

// Comment this out temporarily if you have issues with local certificates
// app.UseHttpsRedirection();

app.MapHealthChecks("/healthz").AllowAnonymous();

app.UseCors("DynamicCorsPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes
app.MapControllers();

app.Run();
