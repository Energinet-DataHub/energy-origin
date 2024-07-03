using System;

namespace API.Models;

public class Terms : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public string Version { get; private set; } = null!;

    private Terms() { }

    public static Terms Create(string version)
    {
        return new Terms
        {
            Id = Guid.NewGuid(),
            Version = version,
        };
    }
}
