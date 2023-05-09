using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.ApiModels.Requests;
public class SubjectIdRequest
    {
        [Required]
        [JsonPropertyName("subjectId")]
        public Guid SubjectId { get; set; }
    }
