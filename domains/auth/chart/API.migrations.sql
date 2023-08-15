CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230203091258_AddUserTable') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "ProviderId" text NOT NULL,
        "Name" text NOT NULL,
        "AcceptedTermsVersion" integer NOT NULL,
        "Tin" text NULL,
        "AllowCPRLookup" boolean NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230203091258_AddUserTable') THEN
    CREATE INDEX "IX_Users_ProviderId" ON "Users" ("ProviderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230203091258_AddUserTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230203091258_AddUserTable', '7.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    DROP INDEX "IX_Users_ProviderId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    ALTER TABLE "Users" DROP COLUMN "ProviderId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    ALTER TABLE "Users" DROP COLUMN "Tin";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    CREATE TYPE provider_key_type AS ENUM ('pid', 'rid', 'mit_id_uuid');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    ALTER TABLE "Users" ADD "CompanyId" uuid NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    CREATE TABLE "Companies" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Tin" text NOT NULL,
        CONSTRAINT "PK_Companies" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    CREATE TABLE "UserProviders" (
        "Id" uuid NOT NULL,
        "ProviderKeyType" provider_key_type NOT NULL,
        "UserProviderKey" text NOT NULL,
        "UserId" uuid NOT NULL,
        CONSTRAINT "PK_UserProviders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserProviders_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    CREATE INDEX "IX_Users_CompanyId" ON "Users" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    CREATE UNIQUE INDEX "IX_Companies_Tin" ON "Companies" ("Tin");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    CREATE UNIQUE INDEX "IX_UserProviders_ProviderKeyType_UserProviderKey" ON "UserProviders" ("ProviderKeyType", "UserProviderKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    CREATE INDEX "IX_UserProviders_UserId" ON "UserProviders" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230328142531_AddCompanyAndUserProviderTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230328142531_AddCompanyAndUserProviderTable', '7.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230413105444_AddEiaProviderKeyTypeEnum') THEN
    ALTER TYPE provider_key_type ADD VALUE 'eia' BEFORE 'mit_id_uuid';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230413105444_AddEiaProviderKeyTypeEnum') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230413105444_AddEiaProviderKeyTypeEnum', '7.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230424102319_RenameColumn') THEN
    ALTER TABLE "Users" RENAME COLUMN "AllowCPRLookup" TO "AllowCprLookup";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230424102319_RenameColumn') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230424102319_RenameColumn', '7.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230815131520_RolesAndTerms') THEN
    ALTER TABLE "Users" DROP COLUMN "AcceptedTermsVersion";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230815131520_RolesAndTerms') THEN
    CREATE TYPE company_terms_type AS ENUM ('terms_of_service');
    CREATE TYPE user_terms_type AS ENUM ('privacy_policy');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230815131520_RolesAndTerms') THEN
    CREATE TABLE "CompanyTerms" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "Type" company_terms_type NOT NULL,
        "AcceptedVersion" integer NOT NULL,
        CONSTRAINT "PK_CompanyTerms" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CompanyTerms_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230815131520_RolesAndTerms') THEN
    CREATE TABLE "UserRoles" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Role" text NOT NULL,
        CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230815131520_RolesAndTerms') THEN
    CREATE TABLE "UserTerms" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Type" user_terms_type NOT NULL,
        "AcceptedVersion" integer NOT NULL,
        CONSTRAINT "PK_UserTerms" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserTerms_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230815131520_RolesAndTerms') THEN
    CREATE UNIQUE INDEX "IX_CompanyTerms_CompanyId_Type" ON "CompanyTerms" ("CompanyId", "Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230815131520_RolesAndTerms') THEN
    CREATE UNIQUE INDEX "IX_UserRoles_UserId_Role" ON "UserRoles" ("UserId", "Role");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230815131520_RolesAndTerms') THEN
    CREATE UNIQUE INDEX "IX_UserTerms_UserId_Type" ON "UserTerms" ("UserId", "Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230815131520_RolesAndTerms') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230815131520_RolesAndTerms', '7.0.3');
    END IF;
END $EF$;
COMMIT;

