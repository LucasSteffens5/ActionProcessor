using ActionProcessor.Api.Endpoints;
using ActionProcessor.Api.Validators;
using ActionProcessor.Application.Handlers;
using ActionProcessor.Domain.Interfaces;
using ActionProcessor.Infrastructure.ActionHandlers;
using ActionProcessor.Infrastructure.BackgroundServices;
using ActionProcessor.Infrastructure.Data;
using ActionProcessor.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using ActionProcessor.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/actionprocessor-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Action Processor API",
        Version = "v1",
        Description = "API for batch processing of actions with external system integration"
    });
});

// Configure JSON serialization for API responses (keep enums as strings for compatibility)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Database
builder.Services.AddDbContext<ActionProcessorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());

// Repositories
builder.Services.AddScoped<IBatchRepository, BatchRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();

// Application Services
builder.Services.AddScoped<FileCommandHandler>();
builder.Services.AddScoped<RetryEventsFailedCommandHandler>();
builder.Services.AddScoped<FileQueryHandler>();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<UploadFileCommandValidator>();

// Action Handlers
builder.Services.AddScoped<SampleActionHandler>();
builder.Services.AddScoped<IActionHandlerFactory, ActionHandlerFactory>();

// HTTP Client for external API calls
builder.Services.AddHttpClient<SampleActionHandler>(client => { client.Timeout = TimeSpan.FromSeconds(30); });

// Background Services
builder.Services.AddHostedService<EventProcessorService>();

// Register endpoints
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

// CORS (if needed for frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Action Processor API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Request logging
app.UseSerilogRequestLogging();

// Map endpoints
app.MapEndpoints();

// Apply database migrations
await MigrateAsync(app);

try
{
    Log.Information("Starting Action Processor API");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    await Log.CloseAndFlushAsync();
}

async Task MigrateAsync(WebApplication webApplication)
{
    using var scope = webApplication.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ActionProcessorDbContext>();
    await context.Database.MigrateAsync();
    Log.Information("Database migrations applied successfully");
}