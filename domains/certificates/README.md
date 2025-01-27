# Certificates Domain

This is the certificates domain.

## Key generation

An Ed25519 key must be generated for the issuing body of certificates, which in this case is Energinet. Project Origin Registry requires that the signature algorithm is Ed25519. To be consistent with the Auth domain, the format used is PEM. The following generates a key and does base64 encoding:

```
openssl genpkey -algorithm ed25519 | base64 -w 0
```

A key is generated and added as a sealed secret in eo-base-environment. This is the private key. At this point in time, the same key is used for all Energy Origin environments.

Project Origin Registry must know the public key for the issuer for the relevant grid areas (e.g. "DK1" and "DK2") and the format here must be "RawPublicKey" (see <https://nsec.rocks/docs/api/nsec.cryptography.keyblobformat>).

## For local development

In order to test and develop locally, enter the docker-environment and run:

```
docker-compose up
```

When shutting down, run:

```
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
dotnet run --project Query.API/API/API.csproj --migrate
```
