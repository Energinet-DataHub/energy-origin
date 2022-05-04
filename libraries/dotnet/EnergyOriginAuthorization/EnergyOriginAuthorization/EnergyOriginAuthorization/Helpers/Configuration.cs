namespace EnergyOriginAuthorization.Helpers
{
    public static class Configuration
    {
        private const string DotNetEnvironment = "ASPNETCORE_ENVIRONMENT";

        public static bool IsDevelopment()
        {
            return "Development".Equals(Environment.GetEnvironmentVariable(DotNetEnvironment), StringComparison.OrdinalIgnoreCase);
        }
    }
}
