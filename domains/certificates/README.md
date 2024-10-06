# Certificates Domain
This is the certificates domain.

## Key generation

An Ed25519 key must be generated for the issuing body of certificates, which in this case is Energinet. Project Origin Registry requires that the signature algorithm is Ed25519. To be consistent with the Auth domain, the format used is PEM. The following generates a key and does base64 encoding:

```
openssl genpkey -algorithm ed25519 | base64 -w 0
```

A key is generated and added as a sealed secret in eo-base-environment. This is the private key. At this point in time, the same key is used for all Energy Origin environments.

Project Origin Registry must know the public key for the issuer for the relevant grid areas (e.g. "DK1" and "DK2") and the format here must be "RawPublicKey" (see https://nsec.rocks/docs/api/nsec.cryptography.keyblobformat).

## For local development
In order to test and develop locally, enter the docker-environment and run:
```
docker-compose up
```
When shutting down, run:
```
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

Updating your local database started with Docker Compose can be done using this command:

```shell
dotnet ef database update --project Shared/DataContext
```

The `--project` argument can be omitted if the working directory is changed to the folder containing the DbContext. The API project and the Worker project folder can be used for some commands, but not all (e.g. adding a migration).

Please refer to the official documentation for more details on the CLI tools for EF Core.

### Updating the database with the migrations

For local development against your Postgres database running using Docker Compose, you must update the database by running e.g. `dotnet ef database update`.

For the integration test project, the migrations are automatically applied as part of the `WebApplicationFactory`.

When running in k8s migrations are applied in an initContainer before the actual application is started. A migration script must be generated for this to work, see [below](#important).

### Important! You must remember this!<a id="important"></a>

You must manually remember to generate the complete SQL migration script after adding a migration. The complete SQL migration script is used to migrate the database when running in k8s.

This is the commands for generating the migration SQL script for the API project and Worker project:

```shell
dotnet ef migrations script --idempotent --project Query.API/API --output migrations/API.sql
```
