using System.Text.Json.Serialization;
using CleanAspire.Api;
using CleanAspire.Application;
using CleanAspire.Application.Common.Services;
using CleanAspire.Domain.Identities;
using CleanAspire.Infrastructure;
using CleanAspire.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using Microsoft.OpenApi;
using CleanAspire.Api.Identity;
using Microsoft.Extensions.FileProviders;
using CleanAspire.Api.Endpoints;
using CleanAspire.Infrastructure.Configurations;
using Microsoft.AspNetCore.Http.Features;
using CleanAspire.Api.ExceptionHandlers;
using CleanAspire.Api.Webpushr;
using Serilog;

// Set up early logging to catch startup errors
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("/app/startup_log.txt")
    .MinimumLevel.Debug()
    .CreateBootstrapLogger();

try 
{
    Log.Information("Starting CleanAspire API");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Log all configuration values to help debug
    foreach (var config in builder.Configuration.AsEnumerable())
    {
        if (!config.Key.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase) && 
            !config.Key.Contains("Password", StringComparison.OrdinalIgnoreCase) && 
            !config.Key.Contains("Secret", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Config: {Key} = {Value}", config.Key, config.Value);
        }
        else
        {
            Log.Information("Config: {Key} = [REDACTED]", config.Key);
        }
    }

    Log.Information("Configuring Serilog");
    builder.RegisterSerilog();

    Log.Information("Configuring Webpushr options");
    builder.Services.Configure<WebpushrOptions>(builder.Configuration.GetSection(WebpushrOptions.Key));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    }).AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration.GetValue<string>("Authentication:Google:ClientId") ?? string.Empty;
        googleOptions.ClientSecret = builder.Configuration.GetValue<string>("Authentication:Google:ClientSecret") ?? string.Empty; ;
    })
        .AddIdentityCookies();
    builder.Services.AddAuthorizationBuilder();
    builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSender>();

    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
            var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
            if (activity != null)
            {
                context.ProblemDetails.Extensions.TryAdd("traceId", activity.Id);
            }
        };
    });
    builder.Services.AddExceptionHandler<ProblemExceptionHandler>();

    builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
    })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddApiEndpoints();
    builder.Services.AddAntiforgery();
    // add a CORS policy for the client
    var allowedCorsOrigins = builder.Configuration.GetValue<string>("AllowedCorsOrigins")?
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? new[] { "https://localhost:7341", "https://localhost:7123", "https://cleanaspire.blazorserver.com" };
    builder.Services.AddCors(
        options => options.AddPolicy(
            "wasm",
            policy => policy.WithOrigins(allowedCorsOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()));


    builder.Services.Scan(scan => scan
        .FromAssemblyOf<Program>()
        .AddClasses(classes => classes.AssignableTo<IEndpointRegistrar>())
        .As<IEndpointRegistrar>()
        .WithScopedLifetime());

    builder.Services.AddOpenApi(options =>
    {
        options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
        options.UseCookieAuthentication();
        options.UseExamples();
    });
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        // Don't serialize null values
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Pretty print JSON
        options.SerializerOptions.WriteIndented = true;
    });
    builder.Services.AddServiceDiscovery();

    // Add service defaults & Aspire client integrations.
    Log.Information("Adding service defaults");
    builder.AddServiceDefaults();
    // Add services to the container.
    builder.Services.AddProblemDetails();

    Log.Information("Building application");
    var app = builder.Build();

    try 
    {
        Log.Information("Initializing database");
        await app.InitializeDatabaseAsync();
        Log.Information("Database initialization completed");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Database initialization failed");
        throw;
    }
    
    // Configure the HTTP request pipeline.
    app.UseExceptionHandler();
    app.MapEndpointDefinitions();
    app.UseCors("wasm");
    app.UseAntiforgery();
    app.Use(async (context, next) =>
    {
        var currentUserContextSetter = context.RequestServices.GetRequiredService<ICurrentUserContextSetter>();
        try
        {
            currentUserContextSetter.SetCurrentUser(context.User);
            await next.Invoke();
        }
        finally
        {
            currentUserContextSetter.Clear();
        }
    });

    Log.Information("Mapping default endpoints");
    app.MapDefaultEndpoints();

    Log.Information("Mapping identity endpoints");
    app.MapIdentityApi<ApplicationUser>();
    app.MapIdentityApiAdditionalEndpoints<ApplicationUser>();

    if (app.Environment.IsDevelopment())
    {
        Log.Information("Development environment detected, mapping OpenAPI");
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    try 
    {
        var filesPath = Path.Combine(Directory.GetCurrentDirectory(), @"files");
        Log.Information("Setting up files directory at {FilesPath}", filesPath);
        
        if (!Directory.Exists(filesPath))
        {
            Log.Information("Creating files directory");
            Directory.CreateDirectory(filesPath);
        }
        
        Log.Information("Configuring static files middleware");
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(filesPath),
            RequestPath = new PathString("/files")
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to set up files directory");
        // Continue anyway - this shouldn't prevent the app from starting
    }

    // Add an explicit health check endpoint for the Docker container
    app.MapGet("/docker-health", () => 
    {
        Log.Information("Health check endpoint called");
        return Results.Ok("Healthy");
    }).AllowAnonymous();

    Log.Information("Starting application");
    try 
    {
        await app.RunAsync();
        Log.Information("Application stopped cleanly");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Application terminated unexpectedly");
        throw;
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}