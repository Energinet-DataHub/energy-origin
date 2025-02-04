using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EnergyOrigin.Domain.ValueObjects.Converters;

public class OrganizationIdValueConverter() : ValueConverter<OrganizationId, Guid>(v => v.Value, v => OrganizationId.Create(v));

public class NullableOrganizationIdValueConverter() : ValueConverter<OrganizationId?, Guid?>(v => v != null ? v.Value : null,
    v => v != null ? OrganizationId.Create(v.Value) : OrganizationId.Empty());
