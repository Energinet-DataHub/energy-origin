using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EnergyOrigin.Domain.ValueObjects.Converters;

public class OrganizationNameValueConverter() : ValueConverter<OrganizationName, string>(v => v.Value, v => String.IsNullOrWhiteSpace(v) ? OrganizationName.Empty() : OrganizationName.Create(v));
