using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using API.Reports;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using Xunit;

namespace API.UnitTests.Reports;

public class CertificatesSpreadsheetExporterTest
{
    [Fact]
    private async Task Export()
    {
        var start = DateTimeOffset.UtcNow.AddHours(-2).ToOffset(TimeSpan.FromHours(12));
        var end = start.AddHours(1);
        var certificates = new List<GranularCertificate>()
        {
            CreateCertificate(start, end, 100, "DK1", CertificateType.Production,
                new FederatedStreamId { Registry = "energy-origin", StreamId = Guid.NewGuid() }, new Dictionary<string, string> {{"energyTag_ProductionDeviceUniqueIdentification", "57..."}})
        };
        var sut = new CertificatesSpreadsheetExporter(new FakeWalletClient(certificates));

        var spreadsheetData = await sut.Export(Guid.NewGuid(), CancellationToken.None);
        var exportFilename = Path.GetTempPath() + spreadsheetData.Filename;
        File.Delete(exportFilename);

        await File.WriteAllBytesAsync(exportFilename, spreadsheetData.Bytes, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(exportFilename));
        Assert.True(spreadsheetData.Bytes.Length > 0);

        // For manual testing
        //var excelProcess = Process.Start("explorer", new[] { exportFilename });
        //await excelProcess.WaitForExitAsync();
    }

    private static GranularCertificate CreateCertificate(DateTimeOffset start, DateTimeOffset end, uint quantity, string gridArea,
        CertificateType certificateType,
        FederatedStreamId federatedStreamId, Dictionary<string, string> attributes)
    {
        return new GranularCertificate
        {
            Start = start.ToUnixTimeSeconds(),
            End = end.ToUnixTimeSeconds(),
            Quantity = quantity,
            GridArea = gridArea,
            CertificateType = certificateType,
            FederatedStreamId = federatedStreamId,
            Attributes = attributes
        };
    }

    private class FakeWalletClient : IWalletClient
    {
        private readonly List<GranularCertificate> _result;

        public FakeWalletClient(List<GranularCertificate> result)
        {
            _result = result;
        }

        public Task<CreateWalletResponse> CreateWallet(Guid ownerSubject, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<ResultList<WalletRecord>> IWalletClient.GetWallets(Guid ownerSubject, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<CreateExternalEndpointResponse> CreateExternalEndpoint(Guid ownerSubject, WalletEndpointReference walletEndpointReference, string textReference,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<RequestStatus> GetRequestStatus(Guid ownerSubject, Guid requestId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ResultList<WalletRecord>> GetWallets(Guid ownerSubject, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TransferResponse> TransferCertificates(Guid ownerSubject, GranularCertificate certificate, uint quantity, Guid receiverId)
        {
            throw new NotImplementedException();
        }

        public Task<ClaimResponse> ClaimCertificates(Guid ownerSubject, GranularCertificate consumptionCertificate,
            GranularCertificate productionCertificate, uint quantity)
        {
            throw new NotImplementedException();
        }

        public async Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken,
            int? limit, int skip = 0, CertificateType? certificateType = null)
        {
            await Task.CompletedTask;
            var resultList = new ResultList<GranularCertificate>
            { Metadata = new PageInfo { Count = _result.Count, Offset = 0, Limit = Int32.MaxValue, Total = _result.Count }, Result = _result };
            return resultList;
        }

        public Task<DisableWalletResponse> DisableWallet(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<EnableWalletResponse> EnableWallet(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ResultList<Claim>?> GetClaims(Guid ownerSubject, DateTimeOffset? start, DateTimeOffset? end, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
