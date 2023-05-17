# Transfer Agreements Domain
This is the transfer agreeements domain.

## For local development

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
dotnet ef migrations add NameOfMigration --project TransferAgreements.API/API
```

Updating your local database can be done using this command:

```shell
dotnet ef database update --project TransferAgreements.API/API
```

The argument `--project TransferAgreements.API/API` can be omitted if the working directory is changed to TransferAgreements.API/API.

Please refer to the official documentation for more details on the CLI tools for EF Core.

### Important! You must remember this!

You must manually remember to generate the complete SQL migration script when adding migrations. The complete SQL migration script is used to migrate the database when running in k8s.

This is the command for generating the migration SQL script for the API project:

```shell
dotnet ef migrations script --idempotent --project TransferAgreements.API/API --output chart/API.migrations.sql
```
