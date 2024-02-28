# Transfer Agreements Domain
This is the transfer agreeements domain.

## For local development<a id="docker-compose"></a>

In order to develop locally, enter the docker-environment folder and run:

```shell
docker-compose up
```

When shutting down, run:

```shell
docker-compose down --volumes
```

## Working with Entity Framework

We use EF Core with code-first approach. This means that the entities added to the `DbContext` is changed first and database migrations is generated afterwards.

The recommendation is to use the [CLI tools for EF Core](https://learn.microsoft.com/en-us/ef/core/cli/dotnet). This is installed by running:

```shell
dotnet tool install --global dotnet-ef
```

Adding a migration can be done like this:

```shell
dotnet ef migrations add NameOfMigration --project Shared/DataContext/DataContext.csproj --startup-project Transfer.API/API/API.csproj --context DataContext.ApplicationDbContext
```

Updating your local database can be done using this command:

```shell
dotnet ef database update --project Shared/DataContext/DataContext.csproj --startup-project Transfer.API/API/API.csproj --context DataContext.ApplicationDbContext
```

The argument `--project TransferAgreements.API/API` can be omitted if the working directory is changed to TransferAgreements.API/API.

Please refer to the official documentation for more details on the CLI tools for EF Core.

### Updating the database with the migrations

For local development against your Postgres database running using [Docker Compose](#docker-compose), you must update the database by running e.g. `dotnet ef database update`.

For the integration test project, the migrations are automatically applied as part of the `WebApplicationFactory`.

When running in k8s migrations are applied in an initContainer before the actual application is started. A migration script must be generated for this to work, see [below](#important).

### Important! You must remember this!<a id="important"></a>

You must manually remember to generate the complete SQL migration script after adding a migration. The complete SQL migration script is used to migrate the database when running in k8s.

This is the command for generating the migration SQL script for the API project:

```shell
dotnet ef migrations script --idempotent --project Transfer.API/API --output migrations/API.sql
```

## API Versioning Strategy

### Overview
In the Transfer Agreements Domain, we implement a strategic approach to API versioning. This is particularly crucial for managing breaking changes to the API contract, ensuring backward compatibility, and facilitating smooth transitions between API versions.

### Versioning Methodology
Our versioning process is designed to handle breaking changes effectively.
Say you want to create a new api version. The following steps are taken:

1. **Duplication of the endpoint with the breaking change:**

    - We start by duplicating the endpoint, with the breaking change and its associated data transfer objects (DTOs), if necessary.
    - Necessary modifications are then applied to these duplicates, to incorporate the required changes.
    - The new endpoint is then versioned by appending the new version number to the `ApiVersion` annotation. An example is shown below:
      ```csharp
      [ApiVersion("20240101")] # <--- Append new version number to the annotation
      [Route("api/claim-automation")]
      ```
    - Since a new API version has been created, the other endpoints, that are not affected by the breaking change, need to be have their versions updated. This is done in the controller class's `ApiVersion` annotation. An example is shown below:
      ```csharp
      [Authorize]
      [ApiController]
      [ApiVersion("20230101")] # <--- Update version number to the annotation
      [Route("api/claim-automation")]
      ```

2. **Deprecation of Old Controllers:**
   If the old endpoint is to be deprecated, the following steps are taken:
    - Previous versions of the controllers are marked as deprecated by setting the `Deprecated = true` flag in the `ApiVersion` annotation. An example is shown below:
      ```csharp
      [Authorize]
      [ApiController]
      [ApiVersion("20230101", Deprecated = true)] # <--- Append flag to the annotation
      [Route("api/claim-automation")]
      ```
3. **Example of Versioned Endpoints:**

```csharp
    [Authorize]
    [ApiController]
    [ApiVersion("20230101", Deprecated = true)] # <--- Old Version remains the same, but is marked as deprecated.
    [ApiVersion("20240101")] # <--- New version added, to indicate controller has endppoints for this version as well.
    [Route("api/claim-automation")]
    public class ClaimAutomationController : ControllerBase
    {

    // Old GET endpoint, available only in the deprecated version.
    [ApiVersion("20230101")] # <--- Explicitly state that the old GET endpoint, is to only appear in the old version
    [HttpGet]
    public async Task<> GetClaimAutomationsOldVersion()
    {
        // Implementation for the old version...
    }

    // New GET endpoint, available only in the new version.
    [ApiVersion("20240101")] # <--- Explicitly state that the new GET endpoint, is to only appear in the new version
    [HttpGet]
    public async Task<ActionResult> GetClaimAutomationsNewVersion()
    {
        // Implementation for the new version...
    }

    // POST endpoint, available in both versions as it has no breaking changes.
    [HttpPost] # <--- Notice the POST endpoint is not explicitly versioned, this means it is available in both versions
    public async Task<ActionResult<ClaimAutomationDto>> PostClaimAutomation() # <--- This endpoint is not duplicated
    {
        // Implementation common to both versions...
    }
}
   ```

### Testing Strategy for New API Versions
Our approach to testing new API versions involves:

- Creating new test Class for the tests of the new API versioned endpoints.
- Writing tests within that class specifically for the new API version, containing only the relevant tests.

This selective testing strategy aligns with our API versioning approach, maintaining efficiency and focus in our testing framework.
