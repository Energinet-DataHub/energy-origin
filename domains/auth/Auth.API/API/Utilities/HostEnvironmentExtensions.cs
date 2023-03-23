namespace API.Utilities
{
    public static class HostEnvironmentExtensions
    {
        public static bool IsTest(this IHostEnvironment hostEnvironment) => hostEnvironment.IsEnvironment("Test");
    }
}
