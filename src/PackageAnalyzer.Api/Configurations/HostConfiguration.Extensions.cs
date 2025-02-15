using System.Reflection;
using Backbone.Comms.Infra.EventBus.MassTransit.DependencyInjection.Configurations;
using Backbone.Comms.Infra.Mediator.MassTransit.DependencyInjection.Configurations;
using Backbone.Comms.Infra.Mediator.MediatR.DependencyInjection.Configurations;
using Backbone.Documentations.TextTemplates.DependencyInjection.Configurations;
using Backbone.General.CoreApp.Abstractions.Settings;
using Backbone.General.DependencyInjection.Abstractions.Attributes;
using Backbone.Language.Core.Time.Provider.Basic.DependencyInjection.Configurations;
using Backbone.Language.Features.Serialization.Json.Newtonsoft.DependencyInjection.Configurations;
using Backbone.Storage.Cache.InMemory.Lazy.DependencyInjection.Configurations;
using FluentValidation;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Octokit;
using PackageAnalyzer.Api.Middleware;
using PackageAnalyzer.Api.Temporary;
using PackageAnalyzer.Domain.Common.Temp.Enums;
using PackageAnalyzer.Domain.Common.Temp.Extensions;
using Serilog;

namespace PackageAnalyzer.Api.Configurations;

public static partial class HostConfiguration
{
    private static readonly ICollection<Assembly> Assemblies = Assembly
        .GetExecutingAssembly()
        .GetReferencedAssemblies()
        .Select(Assembly.Load)
        .Append(Assembly.GetExecutingAssembly())
        .ToList();

    /// <summary>
    /// Registers domain infrastructure
    /// </summary>
    public static WebApplicationBuilder AddBaseInfrastructure(this WebApplicationBuilder builder)
    {
        // Register brokers
        builder.Services.AddTimeProvider();

        // Register settings
        var appSettingsSection = builder.Configuration.GetSection(nameof(AppSettings));
        var customAppSettings = appSettingsSection.Get<CustomAppSettings>()!;

        var test = builder.Environment;

        customAppSettings.Environment = Enum.Parse<AppEnvironmentType>(builder.Environment.EnvironmentName);

        customAppSettings.AppUrl = customAppSettings.Environment.IsProd()
            ? Environment.GetEnvironmentVariable(nameof(AppSettings.AppUrl)) ?? customAppSettings.AppUrl
            : customAppSettings.AppUrl;

        var appSettings = new AppSettings
        {
            AppName = customAppSettings.AppName,
            AppUrl = customAppSettings.AppUrl
        };

        builder.Services.AddSingleton(Options.Create(customAppSettings));
        builder.Services.AddSingleton(Options.Create(appSettings));

        return builder;
    }

    /// <summary>
    /// Registers logging services
    /// </summary>
    private static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console()
            .WriteTo.ApplicationInsights(
                services.GetRequiredService<TelemetryConfiguration>(),
                TelemetryConverter.Traces));

        return builder;
    }

    /// <summary>
    /// Registers serializers.
    /// </summary>
    public static WebApplicationBuilder AddSerializers(this WebApplicationBuilder builder)
    {
        // Register newtonsoft json serializer
        builder.Services.AddNewtonsoftJsonSerializer();

        return builder;
    }

    /// <summary>
    /// Registers caching
    /// </summary>
    public static WebApplicationBuilder AddCaching(this WebApplicationBuilder builder)
    {
        builder.Services.AddInMemoryCacheStorageWithLazyInMemoryCacheStorage(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Registers mapping services
    /// </summary>
    public static WebApplicationBuilder AddMappers(this WebApplicationBuilder builder)
    {
        builder.Services.AddAutoMapper(Assemblies);

        return builder;
    }

    /// <summary>
    /// Registers infrastructure communication infrastructure.
    /// </summary>
    public static WebApplicationBuilder AddInfraComms(this WebApplicationBuilder builder)
    {
        // Add a mediator pipeline with MediatR
        builder.Services
            .AddMediatRServices(Assemblies,
                (mediatorConfiguration, _) => mediatorConfiguration.AddMediatRPipelineBehaviors())
            .AddMediatorWithMediatR();

        // Add a mediator pipeline with MassTransit and in-memory event bus
        builder.Services
            .AddMassTransitServices(
                Assemblies,
                consumerType => !consumerType.GetCustomAttributes(typeof(ExcludeFromAutoRegistrationAttribute), true)
                    .Any(),
                (config, _) => config.AddInMemoryEventBusWithMassTransit(builder.Services, true));

        return builder;
    }

    /// <summary>
    /// Registers persistence infrastructure
    /// </summary>
    public static WebApplicationBuilder AddPersistence(this WebApplicationBuilder builder)
    {
        // Register settings
        // services.AddSingleton(provider =>
        // {
        //     var dbSettingsSection = configuration.GetSection(nameof(DatabaseSettings));
        //     var dbSettings = dbSettingsSection.Get<DatabaseSettings>()!;
        //
        //     var appSettings = provider.GetRequiredService<IOptions<CustomAppSettings>>().Value;
        //
        //     dbSettings.ConnectionString = appSettings.Environment.IsProd()
        //         ? Environment.GetEnvironmentVariable(DataAccessConstants.DefaultDatabaseConnectionString)!
        //         : configuration.GetConnectionString(DataAccessConstants.DefaultDatabaseConnectionString)!;
        //
        //     return Options.Create(dbSettings);
        // });
        //
        // // Register db context
        // services.AddDbContext<AppDbContext>((x, options) =>
        // {
        //     var dbSettings = x.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        //     var appSettings = x.GetRequiredService<IOptions<CustomAppSettings>>().Value;
        //
        //     // Enable detailed errors for dev envs
        //     if (!appSettings.Environment.IsProd())
        //     {
        //         options.EnableDetailedErrors();
        //         options.EnableSensitiveDataLogging();
        //     }
        //
        //     options.UseNpgsql(dbSettings.ConnectionString);
        // });

        return builder;
    }

    /// <summary>
    /// Configures the Dependency Injection container to include validators from referenced assemblies.
    /// </summary>
    public static WebApplicationBuilder AddValidators(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssemblies(Assemblies);

        return builder;
    }

    /// <summary>
    /// Registers file storage infrastructure
    /// </summary>
    public static WebApplicationBuilder AddTemplatesInfrastructure(this WebApplicationBuilder builder)
    {
        // Register settings
        builder.Services.AddBasicTextTemplatesInfrastructure(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Registers github integration
    /// </summary>
    public static WebApplicationBuilder AddGithubIntegration(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<GitHubClient>(_ =>
            new GitHubClient(new ProductHeaderValue("BackbonePackagesService", "v1")));

        return builder;
    }

    /// <summary>
    /// Registers CORS with policy
    /// </summary>
    public static WebApplicationBuilder AddCustomCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend",
                policyBuilder =>
                {
                    policyBuilder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
        });

        return builder;
    }

    /// <summary>
    /// Registers developer tools
    /// </summary>
    public static WebApplicationBuilder AddDevTools(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    []
                }
            });
        });

        return builder;
    }

    /// <summary>
    /// Registers API exposers
    /// </summary>
    public static WebApplicationBuilder AddExposers(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ExceptionFormatter>();

        builder.Services.Configure<ApiBehaviorOptions>(
            options => { options.SuppressModelStateInvalidFilter = true; }
        );

        builder.Services.AddRouting(options => options.LowercaseUrls = true);

        builder.Services
            .AddControllers(options => { options.Filters.Add<GlobalExceptionFilter>(); })
            .AddNewtonsoftJson();

        return builder;
    }

    /// <summary>
    /// Migrates database schemas
    /// </summary>
    public static async ValueTask<WebApplication> MigrateDataBaseSchemasAsync(this WebApplication app)
    {
        // var dbSettings = app.Services.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        //
        // if (dbSettings.MigrateOnStartup)
        //     await app.Services.CreateScope().ServiceProvider.MigrateAsync<AppDbContext>();

        return app;
    }

    /// <summary>
    /// Registers local file storage
    /// </summary>
    public static WebApplication UseLocalFileStorage(this WebApplication app)
    {
        var appPath = app.Environment.ContentRootPath;
        var wwwrootPath = Path.Combine(appPath, "wwwroot");

        if (!Directory.Exists(wwwrootPath))
        {
            Directory.CreateDirectory(wwwrootPath);
            throw new ApplicationException("The web root folder was not found and initialized");
        }

        app.UseStaticFiles();

        return app;
    }

    /// <summary>
    /// Registers developer tools middlewares
    /// </summary>
    public static WebApplication UseDevTools(this WebApplication app)
    {
        app.MapGet("/", () => "Hello world, from Backbone Packages Service.");

        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }

    /// <summary>
    /// Registers exposer middlewares
    /// </summary>
    public static WebApplication UseExposers(this WebApplication app)
    {
        app.UseCors("AllowFrontend");
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}