using System;
using System.Text.Json.Serialization;

namespace API.Api.ApiModels.Responses;

public class TransferAgreementResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("startDate")]
        public DateTimeOffset StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTimeOffset EndDate { get; set; }

        [JsonPropertyName("sender")]
        public SubjectResponse Sender { get; set; }

        [JsonPropertyName("receiver")]
        public SubjectResponse Receiver { get; set; }
    }
