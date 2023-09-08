using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

public class ConnectionInvitation
{
    public Guid Id { get; set; }
    public Guid SenderCompanyId { get; set; }
    public string SenderCompanyTin { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTimeOffset CreatedAt { get; set; }
}
