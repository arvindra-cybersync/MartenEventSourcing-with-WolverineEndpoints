using Application.Handlers;
using AspNetCoreRateLimit;
using Infrastructure.Projections;
using Infrastructure.ReadModels;
using JasperFx;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Events.Daemon;
using Marten.Events.Projections;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebApi.Configuration;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

// Require connection string - fail fast if not configured
var connectionString = builder.Configuration.GetConnectionString("MartenDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string 'MartenDb' is not configured. " +
        "For development, use: dotnet user-secrets set \"ConnectionStrings:MartenDb\" \"Host=localhost;Database=MartenEventStore;Username=postgres;Password=YourPassword\" " +
        "For production, use environment variables or Azure Key Vault.");
}

// ------------------------------------------------
//  MARTEN CONFIGURATION
// ------------------------------------------------
builder.Services.AddMarten(opts =>
{
    opts.Connection(connectionString);

    opts.AutoCreateSchemaObjects = AutoCreate.All;

    // Read model document types
    opts.Schema.For<OrderSummary>().Identity(x => x.Id);
    opts.Schema.For<ProductSales>().Identity(x => x.Id);

    // Register projections
    opts.Projections.Add<OrderSummaryProjection>(ProjectionLifecycle.Inline);
    opts.Projections.Add<ProductSalesProjection>(ProjectionLifecycle.Inline);

})
.ApplyAllDatabaseChangesOnStartup()
.UseLightweightSessions()
.AddAsyncDaemon(DaemonMode.HotCold)
.IntegrateWithWolverine();


// ------------------------------------------------
//  WOLVERINE HTTP CONFIGURATION
// ------------------------------------------------
builder.Services.AddWolverineHttp();

builder.Host.UseWolverine(opts =>
{
    // auto transactions for marten + wolverine pipeline
    opts.Policies.AutoApplyTransactions();

    // durable queues for reliability
    opts.Policies.UseDurableLocalQueues();

    // scan all handlers (application.handlers)
    opts.Discovery.IncludeAssembly(typeof(OrderCommandHandlers).Assembly);
    opts.Discovery.IncludeType<OrderCommandHandlers>();
});

// ------------------------------------------------
//  JWT AUTHENTICATION CONFIGURATION
// ------------------------------------------------
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException(
        "JWT SecretKey is not configured. " +
        "For development, use: dotnet user-secrets set \"JwtSettings:SecretKey\" \"YourSecretKeyHere\" " +
        "IMPORTANT: Use a strong secret key (minimum 32 characters) for production.");
}

var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = jwtSettings.ValidateIssuer,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = jwtSettings.ValidateAudience,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = jwtSettings.ValidateLifetime,
        ClockSkew = TimeSpan.Zero
    };
});

// ------------------------------------------------
//  AUTHORIZATION CONFIGURATION
// ------------------------------------------------
builder.Services.AddAuthorization(AuthorizationPolicies.ConfigurePolicies);

// ------------------------------------------------
//  RATE LIMITING CONFIGURATION
// ------------------------------------------------
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ------------------------------------------------
//  API VERSIONING CONFIGURATION
// ------------------------------------------------
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

// ------------------------------------------------
//  ASP.NET CORE CONFIGURATION
// ------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Order Management API",
        Version = "v1",
        Description = "Event-sourced Order Management API with Marten and Wolverine"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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

// ------------------------------------------------
//  HEALTH CHECKS
// ------------------------------------------------
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: new[] { "db", "ready" });

var app = builder.Build();

// ------------------------------------------------
//  SECURITY MIDDLEWARE
// ------------------------------------------------
// Enforce HTTPS redirection
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts(); // HTTP Strict Transport Security
}

// Add security headers
app.Use(async (context, next) =>
{
    // Prevent clickjacking
    context.Response.Headers["X-Frame-Options"] = "DENY";

    // Prevent MIME type sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

    // XSS Protection
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

    // Referrer Policy
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // Content Security Policy (adjust as needed)
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";

    await next();
});

app.UseMiddleware<WebApi.Middlewares.RequestResponseLoggingMiddleware>();

// ------------------------------------------------
//  RATE LIMITING MIDDLEWARE
// ------------------------------------------------
app.UseIpRateLimiting();

// ------------------------------------------------
//  AUTHENTICATION & AUTHORIZATION MIDDLEWARE
// ------------------------------------------------
app.UseAuthentication();
app.UseAuthorization();

// ------------------------------------------------
//  MIDDLEWARE
// ------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map Wolverine HTTP endpoints
app.MapWolverineEndpoints();

// ------------------------------------------------
//  HEALTH CHECK ENDPOINTS
// ------------------------------------------------
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
