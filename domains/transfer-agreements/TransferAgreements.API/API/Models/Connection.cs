using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace API.Models;

[Table(nameof(Connection), Schema = "con")]
[Index(nameof(CompanyAId))]
[Index(nameof(CompanyBId))]
public class Connection
{
    public Guid Id { get; set; }
    public Guid CompanyAId { get; set; }
    public string CompanyATin { get; set; }
    public Guid CompanyBId { get; set; }
    public string CompanyBTin { get; set; }
}
