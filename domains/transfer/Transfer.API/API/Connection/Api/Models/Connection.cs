using System;
using Microsoft.EntityFrameworkCore;

namespace API.Connection.Api.Models;

[Index(nameof(CompanyAId))]
[Index(nameof(CompanyBId))]
public class Connection
{
    public Guid Id { get; set; }
    public Guid CompanyAId { get; set; }
    public string CompanyATin { get; set; } = string.Empty;
    public Guid CompanyBId { get; set; }
    public string CompanyBTin { get; set; } = string.Empty;
}
