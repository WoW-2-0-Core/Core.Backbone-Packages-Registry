using PackageAnalyzer.Domain.Common.Temp.Enums;

namespace PackageAnalyzer.Api.Temporary;

/// <summary>
/// Represents app settings.
/// </summary>
public class CustomAppSettings
{
    public string AppName { get; init; } = null!;

    public string AppUrl { get; set; } = null!;

    public AppEnvironmentType Environment { get; set; }
}