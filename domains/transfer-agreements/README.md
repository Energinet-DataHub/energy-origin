# Transfer Agreements Domain
This is the transfer agreeements domain.

### Migration generation

**TODO:** Document how to add a single migration

Here is an example of how to generate migrations SQL for the API project:

```shell
dotnet ef migrations script --idempotent --project TransferAgreements.API/API --output chart/API.migrations.sql
```
