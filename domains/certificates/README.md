# Certificates Domain
This is the certificates domain.

## Interfaces from workshop

**Please delete from README when this is implemented**

### MasterDataService

```cs
public interface IMasterService{
    MasterData GetMasterData(string gsrn);
}

public record MasterData(
    string GridArea, // stamdata ["DK1", "DK2"]
    MeteringPointType Type, // enum (Production | Consumption)
    Technology Technology, // stamdata - https://www.aib-net.org/sites/default/files/assets/eecs/facts-sheets/AIB-2019-EECSFS-05%20EECS%20Rules%20Fact%20Sheet%2005%20-%20Types%20of%20Energy%20Inputs%20and%20Technologies%20-%20Release%207.7%20v5.pdf
    string MeteringPointOwner
    //byte[] OwnerPublicKey // stamdata - meterpoint owners key
);
```

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
