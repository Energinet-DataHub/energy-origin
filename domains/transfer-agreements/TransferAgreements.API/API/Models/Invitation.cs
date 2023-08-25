using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

public class Invitation
{
    public Guid Id { get; set; }

    public string Url { get; set; }

    public Guid CompanyId { get; set; }

    public string CompanyTin { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTimeOffset CreatedAt { get; set; }
}
