# Guide for 3rd party API usage

## Introduction

This document contains a getting-started guide on how to use the Energy Track & Trace APIs. The intended audience for this guide is developers and technical personal, when needing to onboard a new 3rd party client.

In this document code examples are shown in `C#`, and all requests target the demo environment <https://demo.energytrackandtrace.dk>. Client systems in production must target the production environment <https://energytrackandtrace.dk>.

### Prerequisites

Before following this guide, you need to complete an onboarding process with Energinet Energy Track & Trace (ETT). As part of this process you will receive credentials for machine-to-machine integration. These credentials consist of a `client-id` and a `client-secret`.

The value of `client-secret` must be kept secret at all times. Use appropriate measures to store it securily while developing and deploying client systems.

It is possible to initiate a grant consent user flow on the ETT website from another website. In order for the flow to redirect the user back to the original website after granting consent, ETT will need a valid `redirect-url`. See [Consent](#consent) section. This `redirect-url` can be provided to Energinet as part of the onboarding process.

### OpenAPI

Open API specification of Energy Track & Trace API can be accessed at <https://energytrackandtrace.dk/developer/>.

## Authorization

All requests to Energy Track & Trace endpoints are authorized. All request must contain an Authorization header with a bearer token. Tokens can be obtained using a standard OAuth 2.0 `client-credentials` grant.

### Obtain access token

```mermaid
sequenceDiagram
    participant client as Client
    box rgb(70, 70, 70) Energy Track & Trace
        participant auth as Authentication
        participant api as API
    end

    client->>auth: Client credentials
    auth->>client: Access token
    client->>api: API request (token)
```

### C# example: Obtain access token

```csharp
var tokenEndpointUrl = "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/B2C_1A_ClientCredentials/oauth2/v2.0/token";

var clientId = "<client-id>";
var clientSecret = "<client-secret>";
var scope = "https://datahubeouenerginet.onmicrosoft.com/energy-origin/.default";

using var httpClient = new HttpClient();
var content = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("grant_type", "client_credentials"),
    new KeyValuePair<string, string>("client_id", clientId),
    new KeyValuePair<string, string>("client_secret", clientSecret),
    new KeyValuePair<string, string>("scope", scope)
});

using var response = await httpClient.PostAsync(tokenEndpointUrl, content);
response.EnsureSuccessStatusCode();

var responseBody = await response.Content.ReadFromJsonAsync<TokenResponse>();

var accessToken = responseBody!.AccessToken;
var accessTokenExpiryTime = DateTime.UtcNow.AddSeconds(responseBody.ExpiresIn);
```

## Consent

When making requests to the API, and accessing resources, you will need consent from the organization owning the resources. Consent is granted by a user affiliated with the organization. Consent is granted using the ETT website.

It is possible to initiate the grant consent user flow from another website. This requires a valid `redirect-url` to be registered with ETT.

```mermaid
sequenceDiagram
    actor user as User
    participant client as 3rd Party Website
    box rgb(70, 70, 70) Energy Track & Trace
        participant ett as Website
    end

    client->>user: Redirect browser <br> (ETT Website)
    user->>ett: Login
    user->>ett: Grant consent
    ett->>user: Redirect browser <br> (3rd party Website)
```

In order to start the grant consent user flow, redirect the user to the following page: `https://energytrackandtrace.dk/da/onboarding?client-id=<client-id>&redirect-url=<redirect url>`. The redirect URL may contain parameters to allow state propagation. After completing the flow and granting consent, the user is redirected to `<redirect-url>`.

## API usage

All API endpoints in ETT are versioned using a header based versioning scheme. A request must contain an `X-API-Version` header with a value matching the desired version of the endpoint to use.

### C# example: API request

```csharp
var token = "<access-token>"; // Access token obtained with client-credentials
var organizationId = "<organization-id>"; // Id of organization to act on behalf of

using var httpClient = new HttpClient();
using var request = new HttpRequestMessage(HttpMethod.Get, $"https://demo.energytrackandtrace.dk/wallet-api/wallets?organizationId={organizationId}");
request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
request.Headers.Add("X-API-Version", "20250101");
var response = await httpClient.SendAsync(request);
```

Refer to OpenAPI spec for a description of available API endpoints.

In the example above, an organization id is used to specify which organization to act on behalf of. The available organizations are given by the consents granted to you as a 3rd party client. A list of consenting organizations can be found with a `GET` request to `api/authorization/client/consents`, providing a valid access token that identifies you as a 3rd party client.

### C# example: Get consents

```csharp
var token = "<access-token>"; // Access token obtained with client-credentials

using var httpClient = new HttpClient();
using var request = new HttpRequestMessage(HttpMethod.Get, "https://demo.energytrackandtrace.dk/api/authorization/client/consents");
request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
request.Headers.Add("X-API-Version", "20230101");
var response = await httpClient.SendAsync(request);
```

The result should look something lige the `JSON` response below. A list of organizations that have granted you consent to act on their behalf.

```json
{
  "result":[
    { "organizationId":"645ca01a-7ddd-4d27-ba67-7abc550ce5e3", "organizationName":"Producent A/S"}]
}
```

Use the `organization id` for the corresponding organization when making request.

## Get Metering Points

Obtain a list of metering points owned by an organization by using the <https://demo.energytrackandtrace.dk/developer#tag/MeteringPoints/paths/~1api~1measurements~1meteringpoints/get> endpoint.

## Create Contract

To enable certificates to be generated for a metering point, the metering point needs to be activated. Creating a contract on the metering point activates it. Use the <https://demo.energytrackandtrace.dk/developer#tag/Contracts/paths/~1api~1certificates~1contracts/post> endpoint.

Use a GSRN number from the response described in [Get Metering Points](#get-metering-points). Certificates will be issued while a contract is active, make sure to specify appropriate start and end dates. End date may be `null` to create an open-ended contract.

If the metering point owner does not already have a wallet, a new wallet will be created as part of the request. For both production and consumption certificates a contract is needed.

## Get Wallet

Certificates issued will be transferred the the organizations wallet. To obtain wallet information use the <https://demo.energytrackandtrace.dk/developer#tag/Wallet/paths/~1wallet-api~1wallets/get> endpoint.

## Create Wallet Endpoint

In order to transfer certificates to another wallet. The receiving wallet owner will need to create a wallet endpoint.

Create wallet endpoint using the <https://demo.energytrackandtrace.dk/developer#tag/Wallet/paths/~1wallet-api~1wallets~1%7BwalletId%7D~1endpoints/post> endpoint. Use `Wallet id` from [](#get-wallet) and `organization id` from consent. see [Authorization](#authorization) for information about how to obtain `organization id`.

## Create External Endpoint

The sender wallet owner will need to create an wallet external endpoint.

Use `Version`, `Endpoint` and `PublicKey` from the wallet endpoint response in [Wallet endpoint](#create-wallet-endpoint) to create a wallet external endpoint. Use the <https://demo.energytrackandtrace.dk/developer#tag/Wallet/paths/~1wallet-api~1external-endpoints/post> endpoint.

The response will include the `received id` to use when transferring certifates.

See <https://github.com/project-origin/wallet/blob/main/doc/concepts/wallet.md> for more information about the wallet system.

## Get Certificates

Sender wallet owner will need to identify which certificates to transfer. To get a list of certificates available to transfer use the <https://demo.energytrackandtrace.dk/developer#tag/Certificates/paths/~1wallet-api~1certificates/get> endpoint.

## Transfer Certificate

Given a certificate identification `federatedStreamId` from the response in [Get Certificates](#get-certificates), the certificate can be transferred to the other wallet.

Use the <https://demo.energytrackandtrace.dk/developer#tag/Transfers/paths/~1wallet-api~1transfers/post> endpoint to transfer a slice of the certificate to the receiving wallet. The `receiver id` from [Create External Endpoint](#create-external-endpoint) should be used as `receiver id` in the request.

Specify the amount to transfer to the receiver wallet. If needed the certificate will be sliced to match the amount to transfer.

By default hashed attributes are not included in the transfer. Make sure to include any hashed attributes, in the transfer request, the receiver wallet owner should be able to see.

## Claim Energy

Given a production certificate and a consumption certificate in a wallet, it is possible to claim the produced energy. Use the <https://demo.energytrackandtrace.dk/developer#tag/Claims/paths/~1wallet-api~1claims/post> endpoint to claim two certificates. If needed to certificates will  be sliced automatically to match the amount of quantity that should be claimed.

## Get Claims

Claimed certificates will not be returned in lists of available certificates. To get claimed certificates, use the <https://demo.energytrackandtrace.dk/developer#tag/Claims/paths/~1wallet-api~1claims~1cursor/get> endpoint.
