# eo-measurements

This repository contains the codebase for the measurements domain which is a part of [Energy Origin](https://github.com/Energinet-DataHub/energy-origin).

## Working with the database

We use Entity Framework Core as Object Relational Mapper (<https://learn.microsoft.com/en-us/ef/core/>).

Entities are placed in `Measurements.API\API\MeteringPoints\Api\Models`.

### Schema migration

 Schema migration scripts are placed in `Measurements.API\API\Migrations\Scripts`.

Migration is performed with the `DbUp` tool (<https://dbup.readthedocs.io>). SQL scripts will automatically be applied to the configured database, when running the application with the `--migrate` argument.

When running in k8s migrations are applied in an initContainer before the actual application is started.

For the integration test projects, the migrations are automatically applied as part of the `WebApplicationFactory` or the individual tests.

#### Adding new migration

Scripts are named using the following convention: `<date>-<sequence>-<description.sql>`. Example: `20250121-0001-MassTransitInboxOutbox.sql`. The scripts will be applied to the database in alphabetical order.

#### Updating local database with migrations

Migrate the local database schema with:

```bash
dotnet run --project Measurements.API/API/API.csproj --migrate
```
