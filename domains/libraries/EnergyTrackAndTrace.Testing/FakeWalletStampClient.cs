using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using NSubstitute;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using WalletClient;
using Xunit.Internal;

namespace EnergyTrackAndTrace.Testing;

public class FakeWalletStampClient : IWalletClient, IStampClient
{
    private readonly Dictionary<Guid, List<WalletRecord>> _wallets = new();
    private readonly List<GranularCertificate> _certificates = new();

    public FakeWalletStampClient()
    {

    }

    public Task<CreateWalletResponse> CreateWallet(Guid ownerSubject, CancellationToken cancellationToken)
    {
        var walletRecord = new WalletRecord() { Id = Guid.NewGuid(), PublicKey = Substitute.For<IHDPublicKey>(), DisabledDate = null };
        if (!_wallets.ContainsKey(ownerSubject))
        {
            _wallets.Add(ownerSubject, new List<WalletRecord>());
        }

        _wallets[ownerSubject].Add(walletRecord);
        return Task.FromResult(new CreateWalletResponse() { WalletId = walletRecord.Id });
    }

    public Task<ResultList<WalletRecord>> GetWallets(Guid ownerSubject, CancellationToken cancellationToken)
    {
        if (!_wallets.TryGetValue(ownerSubject, out var result))
        {
            result = new List<WalletRecord>();
        }

        return Task.FromResult(new ResultList<WalletRecord>()
        {
            Metadata = new PageInfo() { Count = result.Count, Limit = int.MaxValue, Offset = 0, Total = result.Count },
            Result = result
        });
    }

    public Task<CreateExternalEndpointResponse> CreateExternalEndpoint(Guid ownerSubject, WalletEndpointReference walletEndpointReference,
        string textReference,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new CreateExternalEndpointResponse() { ReceiverId = Guid.NewGuid() });
    }

    public Task<RequestStatus> GetRequestStatus(Guid ownerSubject, Guid requestId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new RequestStatus());
    }

    public Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken)
    {
        var wallet = _wallets[ownerSubject].Single(w => w.Id == walletId);

        return Task.FromResult(new WalletEndpointReference(1, new Uri("http://eo"), Substitute.For<IHDPublicKey>()));
    }

    public Task<TransferResponse> TransferCertificates(Guid ownerSubject, GranularCertificate certificate, uint quantity, Guid receiverId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TransferResponse() { TransferRequestId = Guid.NewGuid() });
    }

    public Task<ClaimResponse> ClaimCertificates(Guid ownerSubject, GranularCertificate consumptionCertificate,
        GranularCertificate productionCertificate, uint quantity, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ClaimResponse() { ClaimRequestId = Guid.NewGuid() });
    }

    public Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken, int? limit,
        int skip = 0,
        CertificateType? certificateType = null)
    {
        return Task.FromResult<ResultList<GranularCertificate>?>(new ResultList<GranularCertificate>()
        {
            Metadata = new PageInfo() { Count = _certificates.Count, Limit = Int32.MaxValue, Offset = 0, Total = _certificates.Count },
            Result = _certificates
        });
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

    public Task<ResultList<Claim>?> GetClaims(Guid ownerSubject, DateTimeOffset? start, DateTimeOffset? end, TimeMatch timeMatch,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<CreateRecipientResponse> CreateRecipient(WalletEndpointReference walletEndpointReference, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CreateRecipientResponse() { Id = Guid.NewGuid() });
    }

    public Task<IssueCertificateResponse> IssueCertificate(Guid recipientId, string meteringPointId, CertificateDto certificate,
        CancellationToken cancellationToken)
    {
        var combinedAttributes = certificate.ClearTextAttributes;
        certificate.HashedAttributes.ForEach(ha => combinedAttributes.Add(ha.Key, ha.Value));
        _certificates.Add(new GranularCertificate()
        {
            Attributes = combinedAttributes,
            End = certificate.End,
            Quantity = certificate.Quantity,
            Start = certificate.Start,
            CertificateType = certificate.Type,
            GridArea = certificate.GridArea,
            FederatedStreamId = new FederatedStreamId() { Registry = "123", StreamId = certificate.Id }
        });
        return Task.FromResult(new IssueCertificateResponse());
    }
}
