using System.Text;
using LessonsHub.Application.Interfaces;
using LessonsHub.Extensions;
using LessonsHub.Infrastructure.Configuration;
using LessonsHub.Infrastructure.Data;
using LessonsHub.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<LessonsHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

builder.Services.AddCurrentUser();
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

// Add CORS
//
// The allowlist comes from the CORS_ALLOWED_ORIGINS env var (comma-separated)
// when set, with sensible localhost defaults for local dev otherwise.
//
// Defaults cover dev workflows:
//   - 4200: `ng serve` running directly against the .NET API
//   - 4000: Angular SSR Node server in front of the .NET API
//   -   80: Caddy fronting both
//
// In production (e.g. Cloud Run), set CORS_ALLOWED_ORIGINS to the UI's
// public URL, e.g. `https://lessonshub-ui-XXXX.a.run.app`.
var corsOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
        ?? "http://localhost:4200,https://localhost:4200,http://localhost:4000,https://localhost:4000,http://localhost,http://localhost:80")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins(corsOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// Configure LessonsAiApi settings
var aiApiSettings = builder.Configuration
    .GetSection("LessonsAiApi")
    .Get<LessonsAiApiSettings>() ?? new LessonsAiApiSettings();

// Add HttpClient for LessonsAiApiClient.
// IamAuthHandler attaches a Google-issued ID token in environments where ADC
// is available (i.e. Cloud Run). Local runs proceed without it — the local
// Python container has no IAM check, so unauthenticated calls are accepted.
//
// Resilience: 1 retry on transient errors (network blip, 5xx, 408, 429), an
// inner per-attempt timeout shorter than the outer HttpClient.Timeout, and a
// circuit breaker so a broken AI service doesn't get hammered while users
// keep clicking. Tuned conservative on purpose — AI calls are 10-30s, the
// Python service has its own internal quality-retry loop, and stacking
// retries amplifies tail latency + Gemini cost.
builder.Services.AddTransient<IamAuthHandler>();

void ConfigureAiResilience(HttpStandardResilienceOptions opts)
{
    // Inner per-attempt cap. Must be < client.Timeout (which is the total budget).
    opts.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);
    opts.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(aiApiSettings.TimeoutMinutes);

    // 1 retry, 2s base delay with jitter, exponential backoff capped by attempt timeout.
    opts.Retry.MaxRetryAttempts = 1;
    opts.Retry.Delay = TimeSpan.FromSeconds(2);
    opts.Retry.UseJitter = true;
    opts.Retry.BackoffType = DelayBackoffType.Exponential;

    // Open the circuit if half of recent calls failed in a 30s window; stay
    // open for 30s before allowing a probe.
    opts.CircuitBreaker.MinimumThroughput = 5;
    opts.CircuitBreaker.FailureRatio = 0.5;
    opts.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    opts.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
}

builder.Services.AddHttpClient<ILessonsAiApiClient, LessonsAiApiClient>(client =>
{
    client.BaseAddress = new Uri(aiApiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(aiApiSettings.TimeoutMinutes);
})
.AddHttpMessageHandler<IamAuthHandler>()
.AddStandardResilienceHandler(ConfigureAiResilience);

// RAG client hits the same Python service today (separate concern from
// lesson generation, separate interface so a future split is mechanical).
builder.Services.AddHttpClient<IRagApiClient, RagApiClient>(client =>
{
    client.BaseAddress = new Uri(aiApiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(aiApiSettings.TimeoutMinutes);
})
.AddHttpMessageHandler<IamAuthHandler>()
.AddStandardResilienceHandler(ConfigureAiResilience);

// Document storage strategy — Local for docker-compose dev (shared volume),
// Gcs for Cloud Run prod (object store).
var storageSettings = builder.Configuration
    .GetSection("DocumentStorage")
    .Get<DocumentStorageSettings>() ?? new DocumentStorageSettings();
builder.Services.AddSingleton(storageSettings);
if (storageSettings.Strategy.Equals("Gcs", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IDocumentStorage, GcsDocumentStorage>();
}
else
{
    builder.Services.AddScoped<IDocumentStorage, LocalDocumentStorage>();
}

// Per-user API-key resolution + AI cost logging are split out of the HTTP client
// so each has a single responsibility (SRP) and the HTTP layer no longer
// depends on DbContext (DIP).
builder.Services.AddScoped<IUserApiKeyProvider, UserApiKeyProvider>();
builder.Services.AddScoped<IAiCostLogger, AiCostLogger>();

// Configure Pricing into DI
builder.Services.AddSingleton(aiApiSettings);

// Configure JWT settings
var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<ITokenService, TokenService>();

// Google ID-token validation lives behind IGoogleTokenValidator so AuthService
// stays free of the Google.Apis.Auth dependency.
var googleAuthSettings = builder.Configuration
    .GetSection("GoogleAuth")
    .Get<GoogleAuthSettings>() ?? new GoogleAuthSettings();
builder.Services.AddSingleton(googleAuthSettings);
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LessonsHub API",
        Version = "v1",
        Description = "API for LessonsHub educational platform"
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// (SPA is no longer hosted by this service. The Angular SSR app runs in its
// own container at lessonshub-ui:4000; the Caddy reverse-proxy in docker-compose
// presents both behind one origin, so the browser still treats /api/* as
// same-origin.)

var app = builder.Build();

// Apply pending migrations automatically (retry until DB is ready)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LessonsHubDbContext>();
    var retries = 10;
    while (retries > 0)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Npgsql.NpgsqlException)
        {
            retries--;
            if (retries == 0) throw;
            Console.WriteLine("Database not ready, retrying in 3 seconds...");
            Thread.Sleep(3000);
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "LessonsHub API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");

// Allow Google Sign-In popup to communicate with the opener page
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.Remove("Cross-Origin-Opener-Policy");
        context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
        return Task.CompletedTask;
    });
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
