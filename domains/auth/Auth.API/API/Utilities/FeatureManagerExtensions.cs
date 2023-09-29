using Microsoft.FeatureManagement;

namespace API.Utilities;

public static class FeatureManagerExtensions
{
    // IFeatureManager will never be null at runtime. It will only default to true for unit tests.
    public static bool IsEnabled(this IFeatureManager? featureManager, string feature) => featureManager?.IsEnabledAsync(feature).Result ?? true;
}
