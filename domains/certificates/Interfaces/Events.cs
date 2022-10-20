namespace Interfaces;


// measurement
public enum Quality { Measured, Revised, Calculated, Estimated, }

[EventModelVersion("EnergyMeasured", 1)]
public record EnergyMeasured(
    string GSRN, // Metering Point ID
    long DateFrom,
    long DateTo,
    long Quantity, // Wh
    Quality Quality
) : EventModel;

// cert created
[EventModelVersion("CertificateCreated", 1)]
public record CertificateCreated(
    Guid CertificateId, // Guid.newGuid()
    string GridArea, // stamdata
    Period Period, // EnergyMeasured.DateFrom
    Technology Technology, // stamdata
    byte[] OwnerPublicKey, // stamdata - meterpoint owners key
    ShieldedValue<string> ShieldedGSRN  //Group.commit(EnergyMeasured.GSRN)
    ShieldedValue<long> Quantity //Group.commit(EnergyMeasured.Quantity)
) : EventModel{
    string GSRN { get => ShieldedGSRN.value; }
}

public record Period(
    long DateFrom, // EnergyMeasured.DateFrom
    long DateTo, // EnergyMeasured.DateTo
)

public record Technology(
    string FuelCode, // stamdata
    string TechCode, // stamdata
)

public record ShieldedValue<T>(
    T value, // stamdata
    BigInteger r, // group.RandomR()
)

// cert finalized
[EventModelVersion("CertificateIssued", 1)]
public record CertificateIssued(
    Guid CertificateId, // Guid.newGuid()
)

// cert rejected
[EventModelVersion("CertificateRejected", 1)]
public record CertificateRejected(
    Guid CertificateId, // Guid.newGuid()
    string Reason
)

//Masterdataservice interface
public interface IMasterService{
    MasterData GetMasterData(string gsrn);
}

public record MasterData(
    string GridArea, // stamdata ["DK1", "DK2"]
    Type type, // enum (Production | Consumption)
    Technology Technology, // stamdata - https://www.aib-net.org/sites/default/files/assets/eecs/facts-sheets/AIB-2019-EECSFS-05%20EECS%20Rules%20Fact%20Sheet%2005%20-%20Types%20of%20Energy%20Inputs%20and%20Technologies%20-%20Release%207.7%20v5.pdf
    byte[] OwnerPublicKey, // stamdata - meterpoint owners key
);


// ---- udgående besked til register ---  https://github.com/Energinet-DataHub/energy-origin/blob/main/domains/measurements/Measurements.API/API/Models/Measurement.cs

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
// ---- end ---------

