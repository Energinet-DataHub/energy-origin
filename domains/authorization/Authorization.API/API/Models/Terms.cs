using System;

namespace API.Models;

public class Terms : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public string Version { get; private set; } = null!;
    public string Text { get; private set; } = null!;
    public DateTimeOffset EffectiveDate { get; private set; }

    private Terms() { }

    public Terms(string version, string text)
    {
        Id = Guid.NewGuid();
        Version = version;
        Text = text;
        EffectiveDate = DateTimeOffset.UtcNow;
    }
}
