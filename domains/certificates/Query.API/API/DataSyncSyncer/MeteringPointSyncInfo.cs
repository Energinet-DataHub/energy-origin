using System;
using CertificateValueObjects;

namespace API.DataSyncSyncer;

public record MeteringPointSyncInfo(string GSRN, DateTimeOffset StartSyncDate, string MeteringPointOwner, MeteringPointType MeteringPointType);
