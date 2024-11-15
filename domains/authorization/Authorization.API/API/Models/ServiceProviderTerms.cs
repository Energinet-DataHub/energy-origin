using System;

namespace API.Models;

public class ServiceProviderTerms : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public int Version { get; private set; }

    private ServiceProviderTerms() { }

    public static ServiceProviderTerms Create(int version)
    {
        return new ServiceProviderTerms()
        {
            Id = Guid.NewGuid(),
            Version = version,
        };
    }
}
