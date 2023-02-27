using System;
using CertificateEvents.Primitives;
using Marten.Schema;

namespace API.ContractService;

public class CertificateIssuingContract
{
    private const string uniqueIndexName = "unique_index_contract";

    public Guid Id { get; set; }
    [UniqueIndex(IndexType = UniqueIndexType.Computed, IndexName = uniqueIndexName)]
    public string GSRN { get; set; } = "";
    [UniqueIndex(IndexType = UniqueIndexType.Computed, IndexName = uniqueIndexName)]
    public int ContractNumber { get; set; }
    public string GridArea { get; set; } = "";
    public MeteringPointType MeteringPointType { get; set; }
    public string MeteringPointOwner { get; set; } = "";
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset Created { get; set; }
}
