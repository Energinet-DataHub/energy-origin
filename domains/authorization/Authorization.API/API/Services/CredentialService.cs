using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using EnergyOrigin.Setup.Exceptions;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Applications.Item.RemovePassword;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace API.Services;

public interface ICredentialService
{
    public Task<IEnumerable<ClientCredential>> GetCredentials(Guid applicationId, CancellationToken cancellationToken);
    public Task<ConfidentialClientCredetial> CreateCredential(Guid applicationId, CancellationToken cancellationToken);
    public Task DeleteCredential(Guid applicationId, Guid keyId, CancellationToken cancellationToken);
}

public class CredentialService(IGraphServiceClientWrapper graphWrapper) : ICredentialService
{
    private const string TooManyPasswordsErrorCode = "TooManyAppPasswords";

    public async Task<IEnumerable<ClientCredential>> GetCredentials(Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await GetApplication(applicationId, cancellationToken);
        if (application.PasswordCredentials is null)
        {
            return new List<ClientCredential>();
        }

        var clientCredentials = new List<ClientCredential>();
        foreach (var credential in application.PasswordCredentials)
        {
            if (credential.KeyId is null) continue;
            clientCredentials.Add(ClientCredential.Create(credential.Hint, (Guid)credential.KeyId,
                credential.StartDateTime, credential.EndDateTime));
        }

        return clientCredentials;
    }

    public async Task<ConfidentialClientCredetial> CreateCredential(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await GetApplication(applicationId, cancellationToken);

        try
        {
            var utcNow = DateTimeOffset.UtcNow;
            var body = new AddPasswordPostRequestBody
            {
                PasswordCredential = new PasswordCredential
                {
                    DisplayName = "Client Secret",
                    StartDateTime = utcNow,
                    EndDateTime = utcNow.AddYears(1)
                }
            };

            var result = await graphWrapper.AddPassword(application.Id, body, cancellationToken);
            if (result?.KeyId is null || result.SecretText is null)
            {
                throw new BusinessException("Could not create credential for client.");
            }

            return ConfidentialClientCredetial.Create(result.Hint, (Guid)result.KeyId,
                result.StartDateTime,
                result.EndDateTime, result.SecretText);
        }
        catch (ODataError oDataError)
        {
            if (oDataError.Error?.Code == TooManyPasswordsErrorCode)
            {
                throw new BusinessException(
                    "Not allowed to add 3 credentials. You can only add a maximum of 2 credentials. Delete one of the existing credentials in order to add a new one.",
                    oDataError);
            }

            throw new BusinessException("Could not create credential for client.", oDataError);
        }
    }

    public async Task DeleteCredential(Guid applicationId, Guid keyId, CancellationToken cancellationToken)
    {
        var application = await GetApplication(applicationId, cancellationToken);
        if (application.PasswordCredentials is null)
        {
            throw new ResourceNotFoundException(keyId.ToString());
        }

        try
        {
            var requestBody = new RemovePasswordPostRequestBody
            {
                KeyId = keyId
            };

            await graphWrapper.RemovePassword(application.Id, requestBody, cancellationToken);
        }
        catch (ODataError oDataError)
        {
            throw new BusinessException("Could not delete credential for client.", oDataError);
        }
    }

    private async Task<Application> GetApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        try
        {
            var application = await graphWrapper.GetApplication(applicationId.ToString(), cancellationToken);
            if (application is null)
            {
                throw new ResourceNotFoundException(applicationId.ToString());
            }

            return application;
        }
        catch (ODataError oDataError)
        {
            throw new ResourceNotFoundException(applicationId.ToString(), oDataError);
        }
    }
}
