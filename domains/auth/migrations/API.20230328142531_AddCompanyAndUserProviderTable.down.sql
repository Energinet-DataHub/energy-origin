ALTER TABLE "Users" DROP CONSTRAINT "FK_Users_Companies_CompanyId";

DROP TABLE "Companies";

DROP TABLE "UserProviders";

DROP INDEX "IX_Users_CompanyId";

ALTER TABLE "Users" DROP COLUMN "CompanyId";

DROP TYPE provider_key_type;

ALTER TABLE "Users" ADD "ProviderId" text NOT NULL DEFAULT '';

ALTER TABLE "Users" ADD "Tin" text NULL;

CREATE INDEX "IX_Users_ProviderId" ON "Users" ("ProviderId");

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable';

