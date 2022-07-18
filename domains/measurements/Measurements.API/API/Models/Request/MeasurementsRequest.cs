using System.ComponentModel;

namespace API.Models.Request
{
    public class MeasurementsRequest
    {
        public long DateFrom { get; set; }

        public long DateTo { get; set; }

        [DefaultValue(Aggregation.Total)]
        public Aggregation Aggregation { get; set; } = Aggregation.Total;
    }
}
