using System.Collections.Generic;

namespace API.Models.Response
{
    public class MeteringPointsResponse
    {
        public List<MeteringPoint> MeteringPoints { get; }

        public MeteringPointsResponse(List<MeteringPoint> meteringPoints) => MeteringPoints = meteringPoints;
    }
}
