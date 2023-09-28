using Microsoft.FeatureManagement;

namespace API.Utilities;

public static class FeatureManagerExtensions
{
    public static bool IsEnabled(this IFeatureManager? featureManager, string feature)
        => featureManager?.IsEnabledAsync(feature).Result ?? true;
}
