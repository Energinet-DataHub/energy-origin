using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EnergyOrigin.Domain.ValueObjects.Converters;

public class OrganizationIdValueConverter() : ValueConverter<OrganizationId, Guid>(v => v.Value, v => OrganizationId.Create(v));
