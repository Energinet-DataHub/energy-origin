using System;
using System.Text.Json.Serialization;

namespace API.ApiModels.Requests
{
    public class TransferAgreementChangeRequestModel
    {
        [JsonPropertyName("startDate")]
        public long? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public long? EndDate { get; set; }
    }
}
