using System;
using Microsoft.EntityFrameworkCore;

namespace API.Models;

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
