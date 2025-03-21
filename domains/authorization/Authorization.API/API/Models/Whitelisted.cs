using System;
using EnergyOrigin.Domain.ValueObjects;

namespace API.Models;

public class Whitelisted : IEntity<Guid>
{
    private Whitelisted(
        Guid id,
        Tin tin
    )
    {
        Id = id;
        Tin = tin;
    }

    private Whitelisted()
    {
    }

    public Guid Id { get; private set; }
    public Tin Tin { get; private set; } = null!;

    public static Whitelisted Create(
        Tin tin
    )
    {
        return new Whitelisted(
            id: Guid.NewGuid(),
            tin: tin
        );
    }
}
