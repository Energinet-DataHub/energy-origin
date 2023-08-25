using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

public class Invitation
{
    public Guid Id { get; set; }
    public Guid SenderCompanyId { get; set; }
    public string SenderCompanyTin { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
