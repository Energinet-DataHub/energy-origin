﻿CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE TABLE "Clients" (
        "Id" uuid NOT NULL,
        "IdpClientId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Role" integer NOT NULL,
        CONSTRAINT "PK_Clients" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE TABLE "Organizations" (
        "Id" uuid NOT NULL,
        "IdpId" uuid NOT NULL,
        "IdpOrganizationId" uuid NOT NULL,
        "Tin" text NOT NULL,
        "OrganizationName" text NOT NULL,
        CONSTRAINT "PK_Organizations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "IdpId" uuid NOT NULL,
        "IdpUserId" uuid NOT NULL,
        "Name" text NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE TABLE "Consents" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "ConsentDate" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Consents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Consents_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Consents_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE TABLE "Affiliations" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        CONSTRAINT "PK_Affiliations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Affiliations_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Affiliations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE INDEX "IX_Affiliations_OrganizationId" ON "Affiliations" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Affiliations_UserId_OrganizationId" ON "Affiliations" ("UserId", "OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Clients_IdpClientId" ON "Clients" ("IdpClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Consents_ClientId_OrganizationId" ON "Consents" ("ClientId", "OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE INDEX "IX_Consents_OrganizationId" ON "Consents" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Organizations_IdpOrganizationId" ON "Organizations" ("IdpOrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_IdpUserId" ON "Users" ("IdpUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240429074141_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240429074141_InitialCreate', '8.0.4');
    END IF;
END $EF$;
COMMIT;

