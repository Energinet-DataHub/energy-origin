using Microsoft.FeatureManagement;

namespace API.Utilities;

public static class FeatureManagerExtensions
{
    /// <summary>
    /// This is a convenience extension for IsEnabledAsync(), which checks whether a given feature is enabled.
    /// FeatureManager will never be null at runtime, however it will default to true during unit tests.
    /// </summary>
    /// <param name="featureManager">The instance of FeatureManager.</param>
    /// <param name="feature">The name of the feature to check.</param>
    /// <returns>True if the feature is enabled, otherwise false.</returns>
    public static bool IsEnabled(this IFeatureManager? featureManager, string feature) => featureManager?.IsEnabledAsync(feature).Result ?? true;
}
