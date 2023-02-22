namespace API.Models
{
    public class MeteringPointsResponse
    {
        public List<MeteringPoint> MeteringPoints { get; }

        public MeteringPointsResponse(List<MeteringPoint> meteringPoints) => MeteringPoints = meteringPoints;
    }
}
