using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Services;
using ASP;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace API.Report;

public class GeneratorModel(
    IConsumptionService consumptionService,
    IWalletClient walletClient)
    : PageModel
{
    public required _Page_Report_HourlyCoverageViewModel_cshtml.HourlyCoverageViewModel ModelData { get; set; }

    public async Task OnGetAsync(
        OrganizationId organizationId,
        DateTimeOffset from,
        DateTimeOffset to)
    {
        var consTask = consumptionService.GetTotalHourlyConsumption(organizationId, from, to, CancellationToken.None);
        var claimsTask = walletClient.GetClaims(organizationId.Value, from, to, CancellationToken.None);

        await Task.WhenAll(consTask, claimsTask);

        var consumptionHours = consTask.Result;
        var claimsResponse = claimsTask.Result;
        var claimsList = claimsResponse?.Result ?? [];

        var consArr = consumptionHours
            .OrderBy(c => c.HourOfDay)
            .Select(c => (double)c.KwhQuantity)
            .ToArray();
        var productionArr = Enumerable.Range(0, 24)
            .Select(h => (double)claimsList
                .Where(c => DateTimeOffset.FromUnixTimeSeconds(c.UpdatedAt).Hour == h)
                .Sum(c => c.Quantity))
            .ToArray();

        var matched = consArr.Zip(productionArr, (c, p) => Math.Min(c, p)).ToArray();
        var unmatched = consArr.Zip(matched, (c, m) => c - m).ToArray();
        var overmatch = productionArr.Zip(matched, (p, m) => p - m).ToArray();

        ModelData = new _Page_Report_HourlyCoverageViewModel_cshtml.HourlyCoverageViewModel
        {
            ConsumptionHours = consumptionHours,
            Matched = matched,
            Unmatched = unmatched,
            Overmatched = overmatch
        };
    }
}
