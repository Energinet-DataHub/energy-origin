using System;
using CertificateValueObjects;

namespace API.ContractService;

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

    //TODO: Unit test these methods

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
