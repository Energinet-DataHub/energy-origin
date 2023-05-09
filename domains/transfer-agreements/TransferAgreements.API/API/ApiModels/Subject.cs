using System;
using System.ComponentModel.DataAnnotations;

namespace API.ApiModels;

public class Subject
{
    [Required]
    public int Tin { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; }
}
