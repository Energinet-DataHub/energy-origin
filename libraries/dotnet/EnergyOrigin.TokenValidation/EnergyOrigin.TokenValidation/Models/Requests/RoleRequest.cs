namespace EnergyOrigin.TokenValidation.Models.Requests;

public class RoleRequest
{
    public Guid UserId { get; set; }
    public string RoleKey { get; set; } = null!;
}
