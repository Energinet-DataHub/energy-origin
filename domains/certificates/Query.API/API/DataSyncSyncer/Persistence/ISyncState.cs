using System.Threading.Tasks;
using API.ContractService;

namespace API.DataSyncSyncer.Persistence;

public interface ISyncState
{
    Task<long?> GetPeriodStartTime(CertificateIssuingContract contract);
    void SetSyncPosition(SyncPosition syncPosition);
}
