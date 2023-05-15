using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.ApiModels;

    public class TransferAgreement
    {
        [Required]
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [Required]
        [JsonPropertyName("startDate")]
        public DateTimeOffset StartDate { get; set; }

        [Required]
        [JsonPropertyName("endDate")]
        public DateTimeOffset EndDate { get; set; }

        [Required]
        [JsonPropertyName("receiverTin")]
        public int ReceiverTin { get; set; }
    }
