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

## Working with the database

We use Entity Framework Core as Object Relational Mapper (<https://learn.microsoft.com/en-us/ef/core/>).

Entities are placed in `Shared\DataContext\Models`.

### Schema migration

 Schema migration scripts are placed in `Shared\DataContext\Migrations\Scripts`.

Migration is performed with the `DbUp` tool (<https://dbup.readthedocs.io>). SQL scripts will automatically be applied to the configured database, when running the application with the `--migrate` argument.

When running in k8s migrations are applied in an initContainer before the actual application is started.

For the integration test projects, the migrations are automatically applied as part of the `WebApplicationFactory` or the individual tests.

#### Adding new migration

Scripts are named using the following convention: `<date>-<sequence>-<description.sql>`. Example: `20250121-0001-MassTransitInboxOutbox.sql`. The scripts will be applied to the database in alphabetical order.

#### Updating local database with migrations

Migrate the local database schema with:

```bash
dotnet run --project Transfer.API/API/API.csproj --migrate
```
