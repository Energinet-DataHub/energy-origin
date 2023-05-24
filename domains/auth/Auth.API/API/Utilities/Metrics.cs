using System.Diagnostics.Metrics;

namespace API.Utilities;

public class Metrics
{
    public const string Name = "Auth.Metrics";

    private static readonly Meter authMeter = new Meter(Name);

    public readonly Counter<int> LogoutCounter = authMeter.CreateCounter<int>("user-logout");
    public readonly Counter<int> LoginCounter = authMeter.CreateCounter<int>("user-login");
}
