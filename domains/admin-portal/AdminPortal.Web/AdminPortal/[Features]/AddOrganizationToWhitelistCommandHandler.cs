using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.IntegrationEvents.Events.OrganizationWhitelisted;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AdminPortal._Features_;

public class AddOrganizationToWhitelistCommandHandler : IRequestHandler<AddOrganizationToWhitelistCommand, bool>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AddOrganizationToWhitelistCommandHandler> _logger;

    public AddOrganizationToWhitelistCommandHandler(
        IPublishEndpoint publishEndpoint,
        IDistributedCache cache,
        ILogger<AddOrganizationToWhitelistCommandHandler> logger)
    {
        _publishEndpoint = publishEndpoint;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> Handle(AddOrganizationToWhitelistCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var idempotencyKey = $"whitelist_{request.Tin}";

            var existingRequest = await _cache.GetStringAsync(idempotencyKey, cancellationToken);
            if (existingRequest != null)
            {
                _logger.LogInformation("Duplicate request detected for TIN {Tin}, skipping", request.Tin);
                return true;
            }

            var eventId = Guid.NewGuid();
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            var created = DateTimeOffset.UtcNow;

            var integrationEvent = new OrganizationWhitelistedIntegrationEvent(
                eventId,
                traceId,
                created,
                request.Tin
            );

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);

            await _cache.SetStringAsync(
                idempotencyKey,
                eventId.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                },
                cancellationToken);

            _logger.LogInformation("Published OrganizationWhitelistedIntegrationEvent for TIN {Tin} with EventId {EventId}",
                request.Tin, eventId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing OrganizationWhitelistedIntegrationEvent for TIN {Tin}", request.Tin);
            return false;
        }
    }
}

public class AddOrganizationToWhitelistCommand : IRequest<bool>
{
    public string Tin { get; init; } = string.Empty;
}
