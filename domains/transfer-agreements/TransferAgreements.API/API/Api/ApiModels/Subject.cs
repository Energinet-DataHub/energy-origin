using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.Api.ApiModels;
public class Subject
    {
        [Required]
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [Required]
        [StringLength(8, MinimumLength = 8)]
        [JsonPropertyName("tin")]
        public int Tin { get; set; }
    }
