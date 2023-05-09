using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.ApiModels.Requests
{
    public class TransferAgreementCreateRequest
    {
        [Required]
        [JsonPropertyName("startDate")]
        public DateTimeOffset StartDate { get; set; }

        [Required]
        [JsonPropertyName("endDate")]
        public DateTimeOffset EndDate { get; set; }

        [Required]
        [JsonPropertyName("senderId")]
        public Guid SenderId { get; set; }

        [Required]
        [JsonPropertyName("receiverId")]
        public Guid ReceiverId { get; set; }
    }
}
