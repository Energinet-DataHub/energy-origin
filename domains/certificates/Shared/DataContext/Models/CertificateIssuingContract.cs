using System;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;

namespace DataContext.Models;

public class CertificateIssuingContract
{
    public Guid Id { get; set; }
    public int ContractNumber { get; set; } = 0;
    public string GSRN { get; set; } = "";
    public string GridArea { get; set; } = "";
    public MeteringPointType MeteringPointType { get; set; }
    public string MeteringPointOwner { get; set; } = "";
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset Created { get; set; }
    public Guid RecipientId { get; set; }
    public Technology? Technology { get; set; }
    public bool Trial { get; set; }
    public DateTimeOffset? SponsorshipEndDate { get; set; }

    public static CertificateIssuingContract Create(int contractNumber,
        Gsrn gsrn,
        string gridArea,
        MeteringPointType meteringPointType,
        string meteringPointOwner,
        DateTimeOffset startDate,
        DateTimeOffset? endDate,
        Guid recipientId,
        Technology? technology,
        bool trial,
        DateTimeOffset? sponsorshipEndDate = null)
    {
        return new CertificateIssuingContract
        {
            Id = Guid.Empty,
            ContractNumber = contractNumber,
            GSRN = gsrn.Value,
            GridArea = gridArea,
            MeteringPointType = meteringPointType,
            MeteringPointOwner = meteringPointOwner,
            StartDate = startDate,
            EndDate = endDate,
            Created = DateTimeOffset.UtcNow,
            RecipientId = recipientId,
            Technology = technology,
            Trial = trial,
            SponsorshipEndDate = sponsorshipEndDate
        };
    }

    public bool IsExpired()
    {
        return EndDate.HasValue && EndDate.Value < DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Tests if there is any overlap between he period from <paramref name="startDate"/> to <paramref name="endDate"/> and the period of the contract
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns>true if there is overlap</returns>
    public bool Overlaps(DateTimeOffset startDate, DateTimeOffset? endDate)
        => !(startDate >= EndDate || endDate <= StartDate);

    /// <summary>
    /// Tests if there is any overlap between he period from <paramref name="startDate"/> to <paramref name="endDate"/> and the period of the contract
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns>true if there is overlap</returns>
    public bool Overlaps(long startDate, long? endDate) =>
        Overlaps(DateTimeOffset.FromUnixTimeSeconds(startDate), endDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(endDate.Value) : null);

    /// <summary>
    /// Tests if the period from <paramref name="startDate"/> to <paramref name="endDate"/> is completely within the period of the contract
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns>true if completely within</returns>
    public bool Contains(DateTimeOffset startDate, DateTimeOffset endDate)
        => startDate >= StartDate && (EndDate == null || endDate <= EndDate);

    /// <summary>
    /// Tests if the period from <paramref name="startDate"/> to <paramref name="endDate"/> is completely within the period of the contract
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns>true if completely within</returns>
    public bool Contains(long startDate, long endDate)
        => Contains(DateTimeOffset.FromUnixTimeSeconds(startDate), DateTimeOffset.FromUnixTimeSeconds(endDate));
}
