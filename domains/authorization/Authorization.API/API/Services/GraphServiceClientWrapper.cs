using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Applications.Item.RemovePassword;
using Microsoft.Graph.Models;

namespace API.Services;

public interface IGraphServiceClientWrapper
{
    Task<Application?> GetApplication(string? applicationId, CancellationToken cancellationToken);

    Task<PasswordCredential?> AddPassword(string? applicationId, AddPasswordPostRequestBody body,
        CancellationToken cancellationToken);

    Task RemovePassword(string? applicationId, RemovePasswordPostRequestBody body,
        CancellationToken cancellationToken);

    Task DeleteApplication(string? applicationId, CancellationToken cancellationToken);
}

public class GraphServiceClientWrapper(GraphServiceClient graphServiceClient) : IGraphServiceClientWrapper
{
    public async Task<Application?> GetApplication(string? applicationId, CancellationToken cancellationToken)
    {
        return await graphServiceClient
            .ApplicationsWithAppId(applicationId)
            .GetAsync(cancellationToken: cancellationToken);
    }

    public async Task<PasswordCredential?> AddPassword(string? applicationId, AddPasswordPostRequestBody body,
        CancellationToken cancellationToken)
    {
        return await graphServiceClient.Applications[applicationId].AddPassword
            .PostAsync(body, cancellationToken: cancellationToken);
    }

    public async Task RemovePassword(string? applicationId, RemovePasswordPostRequestBody body,
        CancellationToken cancellationToken)
    {
        await graphServiceClient.Applications[applicationId].RemovePassword
            .PostAsync(body, cancellationToken: cancellationToken);
    }

    public async Task DeleteApplication(string? applicationId, CancellationToken cancellationToken)
    {
        await graphServiceClient.Applications[applicationId]
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}
