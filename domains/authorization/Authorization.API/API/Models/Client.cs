using System;

namespace API.Models;

public class Client
{
    public Guid Id { get; set; }
    public string IdpClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Role Role { get; set; }
}
