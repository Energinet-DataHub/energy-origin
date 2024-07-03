using System;

namespace API.Models;

public class Terms : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public string Version { get; private set; } = null!;
    public DateTimeOffset EffectiveDate { get; set; }

    private Terms() { }

    public Terms(string version)
    {
        Id = Guid.NewGuid();
        Version = version;
        EffectiveDate = DateTimeOffset.UtcNow;
    }
}
