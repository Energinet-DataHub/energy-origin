# Certificates Domain
This is the certificates domain.

## Interfaces from workshop

**Please delete from README when this is implemented**

### Registry

Github repo: https://github.com/project-origin/registry/pulls. The branch of interest is `feature/electricity`. Below is copied from the Registry code and is what should be called by our RegistryConnector.

```cs
// CommitmentParameters is defined in ProjectOrigin

public record IssueProduction(
    CommitmentParameters GsrnCommitmentParameters,
    CommitmentParameters AmountCommitmentParameters,
    ProductionIssued Event,
    byte[] Signature  // sign with energinet key Ed25519 from config
    );

public record ProductionIssued(
    Guid CertificateId,
    DateTimeOffset Start,
    DateTimeOffset End,
    string GridArea,
    BigInteger GsrnCommitment,
    BigInteger AmountCommitment,
    string FuelCode,
    string TechCode,
    byte[] OwnerPublicKey,
    CommitmentParameters? AmountParameters = null);

```

### For local development
In-order to test and develop locally, enter the folder [docker test folder](file://./docker-test-env/) and run:
```
docker-compose up
```
When shutting down, run:
```
docker-compose down --volumes
```

