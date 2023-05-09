using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.ApiModels.Responses;
public class SubjectResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("tin")]
        public int Tin { get; set; }
    }
