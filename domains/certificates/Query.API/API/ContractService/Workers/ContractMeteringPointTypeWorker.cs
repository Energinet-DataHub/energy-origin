using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.UnitOfWork;
using DataContext.ValueObjects;
using Energinet.DataHub.Measurements.Abstractions.Api.Models;
using Meteringpoint.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.ContractService.Workers;

public class ContractMeteringPointTypeWorker(
        IServiceScopeFactory serviceScopeFactory,
        Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointsClient,
        ILogger<ContractMeteringPointTypeWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var contracts = await unitOfWork
                .CertificateIssuingContractRepo
                .Query()
                .ToListAsync(stoppingToken);

            var distinctMeteringPointOwners = contracts
                .DistinctBy(x => x.MeteringPointOwner)
                .Select(x => x.MeteringPointOwner);

            var meteringPoints = new Dictionary<string, List<Meteringpoint.V1.MeteringPoint>>();
            foreach (var meteringPointOwner in distinctMeteringPointOwners)
            {
                var request = new OwnedMeteringPointsRequest { Subject = meteringPointOwner };
                var meteringPointsResponse = await meteringPointsClient.GetOwnedMeteringPointsAsync(request, cancellationToken: stoppingToken);
                if (meteringPointsResponse is not null)
                {
                    meteringPoints.Add(meteringPointOwner, [.. meteringPointsResponse.MeteringPoints]);
                }
            }

            foreach (var contract in contracts)
            {
                if (contract.MeteringPointType == MeteringPointType.Production) continue;

                var meteringPointsForContractOwner = meteringPoints
                    .FirstOrDefault(x => x.Key == contract.MeteringPointOwner);

                if (meteringPointsForContractOwner.Value.Count == 0)
                {
                    logger.LogWarning(
                            "Contract job: Contracts MeteringPointOwner does not have any metering points, for {MeteringPointOwner}",
                            contract.MeteringPointOwner);

                    continue;
                }

                var meteringPoint = meteringPointsForContractOwner.Value.FirstOrDefault(x => x.MeteringPointId == contract.GSRN);
                if (meteringPoint is null)
                {
                    logger.LogWarning("Contract job: No MeteringPoint found for GSRN {GSRN}", contract.GSRN);
                    continue;
                }
                var meterType = GetMeterType(meteringPoint.TypeOfMp);
                if (meterType == MeterType.Production || meterType == MeterType.Child)
                {

                    logger.LogWarning(
                            "Contract job: Contract has MeteringPointType {MeteringPointTypeContract} and MeteringPoint has MeteringPointType {MeteringPointTypeMeteringPoint}. GSRN {GSRN}",
                            contract.MeteringPointType.ToString(), meterType.ToString(), contract.GSRN);
                }
            }

        }
        catch (Exception e)
        {
            logger.LogError(e, "Contract job: Something went wrong with the ContractMeteringPointTypeWorker");
        }
    }

    public static MeterType GetMeterType(string typeOfMp)
    {
        if (typeOfMp == "E17")
        {
            return MeterType.Consumption;
        }
        else if (typeOfMp == "E18")
        {
            return MeterType.Production;
        }
        else if (typeOfMp.StartsWith("D") && typeOfMp.Length == 3 && int.Parse(typeOfMp.Substring(1)) >= 1)
        {
            return MeterType.Child;
        }

        throw new NotSupportedException($"TypeOfMP '{typeOfMp}' is not supported.");
    }
}
