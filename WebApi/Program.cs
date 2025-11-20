using Application.Handlers;
using Infrastructure.Projections;
using Infrastructure.ReadModels;
using JasperFx;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Events.Daemon;
using Marten.Events.Projections;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MartenDb")
    ?? "Host=localhost;Port=5432;Database=MartenEventStore;Username=postgres;Password=Postgres@123";

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
//  ASP.NET CORE CONFIGURATION
// ------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseMiddleware<WebApi.Middlewares.RequestResponseLoggingMiddleware>();

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

app.Run();
