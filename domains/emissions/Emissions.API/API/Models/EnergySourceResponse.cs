using System.Text.Json.Serialization;

namespace API.Models
{
    public class EnergySourceResponse
    {
        public EnergySourceResponse(List<EnergySourceDeclaration> energySources)
        {
            EnergySources = energySources;
        }

        public List<EnergySourceDeclaration> EnergySources { get; }
    }
}
