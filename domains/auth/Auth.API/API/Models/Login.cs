namespace API.Models;

public record Login
{
    public string FeUrl { get; init;  }
    public string ReturnUrl { get; init;  }
}
