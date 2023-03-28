using EnergyOrigin.TokenValidation.Utilities;

namespace API.Utilities
{
    public static class UserDescriptorExtensions
    {
        public static string? GetSubject(this UserDescriptor descriptor)
        {
            return descriptor.CompanyId?.ToString() ?? descriptor.Id?.ToString();
        }
    }
}
