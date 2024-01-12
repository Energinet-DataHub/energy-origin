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
dotnet ef migrations add NameOfMigration --project Shared/DataContext
```

Updating your local database can be done using this command:

```shell
dotnet ef database update --project Shared\DataContext\DataContext.csproj --startup-project Transfer.API\API\API.csproj --context DataContext.ApplicationDbContext
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

1. **Duplication of Controllers and DTOs:**

    - We start by duplicating each controller and its associated data transfer objects (DTOs).
    - After duplication the folder structure should look like this, with transfer as example:
   ```
    transfer/
    ├── v2023_01_01/
    │   ├── Controllers/
    │   └── Dto/
    └── v2023_11_11/
        ├── Controllers/
        └── Dto/
   ```
    - Necessary modifications are then applied to these duplicates to incorporate the required changes.

2. **Deprecation of Old Controllers:**
    - Previous versions of the controllers are marked as deprecated by setting the `Deprecated = true` flag in the `ApiVersion` annotation. An example is shown below:
      ```csharp
      [Authorize]
      [ApiController]
      [ApiVersion("20230101", Deprecated = true)] # <--- Append flag to the annotation
      [Route("api/claim-automation")]
      ```

### Streamlining the Versioning Process
To ease the versioning process, we utilize the `create_new_api_version.sh` Bash script:

- This script automates the creation of new controller and DTO folders, following the naming convention "vYYYY_MM_DD" (e.g., "v2023_01_01").
- Note: Manual deprecation of old API controllers is still required after running this script.

### Testing Strategy for New API Versions
Our approach to testing new API versions involves:

- Creating new test folders named according to the new API version.
- Developing test files within these folders specifically for the new API version, containing only the relevant tests.

This selective testing strategy aligns with our API versioning approach, maintaining efficiency and focus in our testing framework.
