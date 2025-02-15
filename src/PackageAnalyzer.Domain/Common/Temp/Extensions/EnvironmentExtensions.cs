using PackageAnalyzer.Domain.Common.Temp.Enums;

namespace PackageAnalyzer.Domain.Common.Temp.Extensions;

/// <summary>
/// Contains extensions for environment
/// </summary>
public static class EnvironmentExtensions
{
    /// <summary>
    /// Checks if environment is production and likely running in the cloud or development and likely running locally.
    /// </summary>
    /// <param name="environmentType"></param>
    /// <returns>True if the environment is set to production; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">If the environment has invalid value.</exception>
    public static bool IsProd(this AppEnvironmentType environmentType)
    {
        return environmentType switch
        {
            AppEnvironmentType.Development or AppEnvironmentType.Testing => false,
            AppEnvironmentType.Production or AppEnvironmentType.Staging => true,
            _ => throw new InvalidOperationException("Unsupported environment type.")
        };
    }
}