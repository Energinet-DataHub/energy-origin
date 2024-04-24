using System;

namespace API.Models;

public class Client
{
    public Guid Id { get; init; }
    public string IdpClientId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Role Role { get; init; }
}
