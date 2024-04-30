using System.Reflection;
using API.ValueObjects;
using AutoFixture;
using AutoFixture.Kernel;
using Bogus;

namespace API.IntegrationTests.Setup;

public class NoCircularReferencesCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(behavior => fixture.Behaviors.Remove(behavior));
    }
}

public class IgnoreVirtualMembersCustomization : ICustomization
{
    public void Customize(IFixture fixture) => fixture.Customizations.Add(new IgnoreVirtualMembers());
}

public class IgnoreVirtualMembers : ISpecimenBuilder
{
    public object? Create(object request, ISpecimenContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var propertyInfo = request as PropertyInfo;
        if (propertyInfo == null)
        {
            return new NoSpecimen();
        }

        if (propertyInfo.GetMethod != null && propertyInfo.GetMethod.IsVirtual)
            return null;

        return new NoSpecimen();
    }
}

public static class ModelsWithDefaults
{
    private static readonly Faker Faker = new();

    public static T CreateWithDefaults<T>(params (Type type, object? value)[] overrides) where T : class
    {
        var createMethod = typeof(T).GetMethod("Create") ?? throw new InvalidOperationException($"Type {typeof(T).Name} does not have a 'Create' method.");

        var parameterValues = createMethod.GetParameters().Select(p => GetParameterValue(p, overrides)).ToArray();

        return (T)createMethod.Invoke(null, parameterValues)!;
    }

    private static object GetParameterValue(ParameterInfo parameter, (Type type, object? value)[] overrides)
    {
        return overrides.FirstOrDefault(o => o.type == parameter.ParameterType).value ?? CreateDefault(parameter.ParameterType);
    }


    private static object CreateDefault(Type type)
    {
        return type switch
        {
            _ when type == typeof(IdpId) => new IdpId(Faker.Random.Guid()),
            _ when type == typeof(IdpOrganizationId) => new IdpOrganizationId(Faker.Random.Guid()),
            _ when type == typeof(IdpClientId) => new IdpClientId(Faker.Random.Guid()),
            _ when type == typeof(IdpUserId) => new IdpUserId(Faker.Random.Guid()),
            _ when type == typeof(Tin) => new Tin(Faker.Random.Replace("########")),
            _ when type == typeof(OrganizationName) => new OrganizationName(Faker.Company.CompanyName()),
            _ when type == typeof(Name) => new Name(Faker.Name.FullName()),
            _ => throw new InvalidOperationException($"Unsupported parameter type: {type.Name}")
        };
    }
}

public class ModelCreationCustomization : ICustomization
{
    private static readonly Faker _faker = new Faker();

    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new SpecimenBuilder());
    }

    private class SpecimenBuilder : ISpecimenBuilder
    {
        public object? Create(object request, ISpecimenContext context)
        {
            if (request is not Type type || type.GetMethod("Create") == null) return new NoSpecimen();

            var parameterValues = type.GetMethod("Create")!.GetParameters()
                .Select(param =>
                {
                    if (param.ParameterType == typeof(IdpId))
                        return new IdpId(Guid.NewGuid());

                    if (param.ParameterType == typeof(IdpOrganizationId))
                        return new IdpOrganizationId(Guid.NewGuid());

                    if (param.ParameterType == typeof(IdpClientId))
                        return new IdpClientId(Guid.NewGuid());

                    if (param.ParameterType == typeof(IdpUserId))
                        return new IdpUserId(Guid.NewGuid());

                    if (param.ParameterType == typeof(Tin))
                        return new Tin(_faker.Random.Replace("########"));

                    if (param.ParameterType == typeof(OrganizationName))
                        return new OrganizationName(_faker.Company.CompanyName());

                    if (param.ParameterType == typeof(Name))
                        return new Name(_faker.Name.FullName());

                    return context.Resolve(param.ParameterType);
                })
                .ToArray();

            return type.GetMethod("Create")!.Invoke(null, parameterValues);
        }
    }
}
