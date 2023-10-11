DROP INDEX "IX_Users_ProviderId";

ALTER TABLE "Users" DROP COLUMN "ProviderId";

ALTER TABLE "Users" DROP COLUMN "Tin";

CREATE TYPE provider_key_type AS ENUM ('pid', 'rid', 'mit_id_uuid');

ALTER TABLE "Users" ADD "CompanyId" uuid NULL;

CREATE TABLE "Companies" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Tin" text NOT NULL,
    CONSTRAINT "PK_Companies" PRIMARY KEY ("Id")
);

CREATE TABLE "UserProviders" (
    "Id" uuid NOT NULL,
    "ProviderKeyType" provider_key_type NOT NULL,
    "UserProviderKey" text NOT NULL,
    "UserId" uuid NOT NULL,
    CONSTRAINT "PK_UserProviders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserProviders_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Users_CompanyId" ON "Users" ("CompanyId");

CREATE UNIQUE INDEX "IX_Companies_Tin" ON "Companies" ("Tin");

CREATE UNIQUE INDEX "IX_UserProviders_ProviderKeyType_UserProviderKey" ON "UserProviders" ("ProviderKeyType", "UserProviderKey");

CREATE INDEX "IX_UserProviders_UserId" ON "UserProviders" ("UserId");

ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230328142531_AddCompanyAndUserProviderTable', '7.0.3');

