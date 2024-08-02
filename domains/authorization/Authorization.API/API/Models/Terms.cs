using System;

namespace API.Models;

public class Terms : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public int Version { get; private set; }

    private Terms() { }

    public static Terms Create(int version)
    {
        return new Terms
        {
            Id = Guid.NewGuid(),
            Version = version,
        };
    }
}
