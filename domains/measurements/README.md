# eo-measurements
This repository contains the codebase for the measurements domain which is a part of [Energy Origin](https://github.com/Energinet-DataHub/energy-origin).

### Updating the database with the migrations

For local development against your Postgres database running using Docker Compose, you must update the database by running e.g. `dotnet ef database update`.


When running in k8s migrations are applied in an initContainer before the actual application is started. A migration script must be generated for this to work, see [below](#important).

### Important! You must remember this!<a id="important"></a>

You must manually remember to generate the complete SQL migration script after adding a migration. The complete SQL migration script is used to migrate the database when running in k8s.


This is the commands for generating the migration SQL script for the API project and Worker project:

```shell
dotnet ef migrations script --idempotent --project Measurements.API/API --output migrations/API.sql
```
