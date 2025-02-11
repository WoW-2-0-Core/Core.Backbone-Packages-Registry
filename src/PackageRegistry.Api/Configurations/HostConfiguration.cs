namespace PackageRegistry.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>
    /// Configures application builder
    /// </summary>
    public static ValueTask<WebApplicationBuilder> ConfigureAsync(this WebApplicationBuilder builder)
    {
        builder
            .AddBaseInfrastructure()
            // .AddMonitoring()
            // .AddLogging()
            .AddSerializers()
            .AddCaching()
            .AddMappers()
            .AddInfraComms()
            .AddPersistence()
            .AddValidators()
            .AddTemplatesInfrastructure()
            .AddGithubIntegration()
            .AddDevTools()
            .AddExposers()
            .AddCustomCors();

        return new ValueTask<WebApplicationBuilder>(builder);
    }

    /// <summary>
    /// Configures application
    /// </summary>
    public static async ValueTask<WebApplication> ConfigureAsync(this WebApplication app)
    {
        await app.MigrateDataBaseSchemasAsync();

        app
            .UseLocalFileStorage()
            .UseDevTools()
            .UseExposers();

        return app;
    }
}