using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.Reports;
using ProjectOriginClients;
using ProjectOriginClients.Models;
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
                new FederatedStreamId { Registry = "energy-origin", StreamId = Guid.NewGuid() }, new Dictionary<string, string> {{"assetId", "57..."}})
        };
        var sut = new CertificatesSpreadsheetExporter(new FakeWalletClient(certificates));

        var spreadsheetData = await sut.Export(Guid.NewGuid(), CancellationToken.None);
        var exportFilename = Path.GetTempPath() + spreadsheetData.Filename;
        File.Delete(exportFilename);

        await File.WriteAllBytesAsync(exportFilename, spreadsheetData.Bytes);

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

        public Task<CreateWalletResponse> CreateWallet(string ownerSubject, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ResultList<WalletRecord>> GetWallets(string ownerSubject, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, string ownerSubject, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken,
            int? limit, int skip = 0)
        {
            await Task.CompletedTask;
            var resultList = new ResultList<GranularCertificate>
            { Metadata = new PageInfo { Count = _result.Count, Offset = 0, Limit = Int32.MaxValue, Total = _result.Count }, Result = _result };
            return resultList;
        }
    }
}
