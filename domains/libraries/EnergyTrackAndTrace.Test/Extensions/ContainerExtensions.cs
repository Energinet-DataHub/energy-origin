using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace EnergyTrackAndTrace.Test.Extensions;

public static class ContainerExtensions
{
    public static async Task StartWithLoggingAsync(this IContainer container)
    {
        try
        {
            await container.StartAsync();
        }
        catch (Exception e)
        {
            var (stdout, stderr) = await container.GetLogsAsync();
            throw new Exception($"Container logs:\nStdout: {stdout}\nStderr:{stderr}", e);
        }
    }

    public static IWaitForContainerOS UntilGrpcEndpointIsReady(this IWaitForContainerOS container, ushort grpcPort, string path, Action<IWaitStrategy>? waitStrategyModifier = null)
    {
        // This is a workaround to check if a grpc endpoint is ready.
        // GRPC uses http2 requests which are not supported as a WaitStrategy.
        // However, the message 'An HTTP/1.x request was sent to an HTTP/2 only endpoint.' indicates that endpoint is actually ready.

        // Default timeout is 1 minute
        waitStrategyModifier ??= ws => ws.WithTimeout(TimeSpan.FromMinutes(1));

        return container.UntilHttpRequestIsSucceeded(s =>
                    s.ForPath(path)
                        .ForPort(grpcPort)
                        .ForStatusCode(HttpStatusCode.BadRequest)
                        .ForResponseMessageMatching(async r =>
                        {
                            var content = await r.Content.ReadAsStringAsync();
                            var isHttp2ServerReady = "An HTTP/1.x request was sent to an HTTP/2 only endpoint.".Equals(content);
                            return isHttp2ServerReady;
                        })
                    , waitStrategyModifier
        );
    }

    public static string GetLocalConnectionString(this PostgreSqlContainer container, string networkAlias)
    {
        var connectionProperties = new Dictionary<string, string>
        {
            { "Host", networkAlias },
            {
                "Port",
                ((ushort)5432).ToString()
            },
            { "Database", "postgres" },
            { "Username", "postgres" },
            { "Password", "postgres" }
        };

        return string.Join(";", connectionProperties.Select((KeyValuePair<string, string> property) => string.Join("=", property.Key, property.Value)));
    }
}
