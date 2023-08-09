using System;
using System.Security.Cryptography;
using Google.Protobuf;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;

namespace ProjectOriginClients;

public static class Registry
{
    public static IssuedEvent CreateIssuedEventForProduction(string registryName, Guid certificateId, DateInterval period, string gridArea, string assetId, SecretCommitmentInfo commitment, IPublicKey ownerPublicKey)
    {
        var id = new ProjectOrigin.Common.V1.FederatedStreamId
        {
            Registry = registryName,
            StreamId = new ProjectOrigin.Common.V1.Uuid { Value = certificateId.ToString() }
        };

        var issuedEvent = new IssuedEvent
        {
            CertificateId = id,
            Type = GranularCertificateType.Production,
            Period = period,
            GridArea = gridArea,
            QuantityCommitment = new ProjectOrigin.Electricity.V1.Commitment
            {
                Content = ByteString.CopyFrom(commitment.Commitment.C),
                RangeProof = ByteString.CopyFrom(commitment.CreateRangeProof(id.StreamId.Value))
            },
            OwnerPublicKey = new PublicKey
            {
                Content = ByteString.CopyFrom(ownerPublicKey.Export()),
                Type = KeyType.Secp256K1
            }
        };

        issuedEvent.Attributes.Add(new ProjectOrigin.Electricity.V1.Attribute
        {
            Key = "AssetId",
            Value = assetId
        });

        return issuedEvent;
    }

    public static SendTransactionsRequest CreateSendTransactionRequest(this IssuedEvent issuedEvent, IPrivateKey issuerKey)
    {
        var header = new TransactionHeader
        {
            FederatedStreamId = issuedEvent.CertificateId,
            PayloadType = IssuedEvent.Descriptor.FullName,
            PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(issuedEvent.ToByteArray())),
            Nonce = Guid.NewGuid().ToString(),
        };

        var headerSignature = issuerKey.Sign(header.ToByteArray()).ToArray();

        var transaction = new Transaction
        {
            Header = header,
            HeaderSignature = ByteString.CopyFrom(headerSignature),
            Payload = issuedEvent.ToByteString()
        };

        var request = new SendTransactionsRequest();
        request.Transactions.Add(transaction);

        return request;
    }

    public static GetTransactionStatusRequest CreateStatusRequest(this Transaction transaction) =>
        new()
        {
            Id = Convert.ToBase64String(SHA256.HashData(transaction.ToByteArray()))
        };
}
