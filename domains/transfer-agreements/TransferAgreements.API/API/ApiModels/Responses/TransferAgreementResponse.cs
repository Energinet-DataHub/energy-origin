using System;
using System.Text.Json.Serialization;

namespace API.ApiModels.Responses
{
    public class TransferAgreementResponseModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("providerName")]
        public string ProviderName { get; set; }

        [JsonPropertyName("providerTin")]
        public long ProviderTin { get; set; }

        [JsonPropertyName("receiverName")]
        public string ReceiverName { get; set; }

        [JsonPropertyName("receiverTin")]
        public long ReceiverTin { get; set; }

        [JsonPropertyName("startDate")]
        public long StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public long EndDate { get; set; }
        public Guid ProviderId { get; internal set; }
        public Guid ReceiverId { get; internal set; }
    }
}
