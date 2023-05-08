using System;
using System.Text.Json.Serialization;

namespace API.ApiModels.Requests
{
    public class TransferAgreementRequestModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("providerId")]
        public Guid ProviderId { get; set; }

        [JsonPropertyName("receiverId")]
        public Guid ReceiverId { get; set; }

        [JsonPropertyName("startDate")]
        public long StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public long EndDate { get; set; }
    }
}
