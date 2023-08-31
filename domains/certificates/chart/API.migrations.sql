CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230830063723_Initial') THEN
    CREATE TABLE "Contracts" (
        "Id" uuid NOT NULL,
        "ContractNumber" integer NOT NULL,
        "GSRN" text NOT NULL,
        "GridArea" text NOT NULL,
        "MeteringPointType" integer NOT NULL,
        "MeteringPointOwner" text NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone NULL,
        "Created" timestamp with time zone NOT NULL,
        "WalletUrl" text NOT NULL,
        "WalletPublicKey" bytea NOT NULL,
        CONSTRAINT "PK_Contracts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230830063723_Initial') THEN
    CREATE UNIQUE INDEX "IX_Contracts_GSRN_ContractNumber" ON "Contracts" ("GSRN", "ContractNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230830063723_Initial') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230830063723_Initial', '7.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230830095336_Cert') THEN
    CREATE TABLE "ProductionCertificates" (
        "Id" uuid NOT NULL,
        "IssuedState" integer NOT NULL,
        "GridArea" text NOT NULL,
        "Period_DateFrom" bigint NOT NULL,
        "Period_DateTo" bigint NOT NULL,
        "Technology_FuelCode" text NOT NULL,
        "Technology_TechCode" text NOT NULL,
        "MeteringPointOwner" text NOT NULL,
        "Gsrn" text NOT NULL,
        "Quantity" bigint NOT NULL,
        "BlindingValue" bytea NOT NULL,
        "RejectionReason" text NULL,
        CONSTRAINT "PK_ProductionCertificates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230830095336_Cert') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230830095336_Cert', '7.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230830104717_Unique') THEN
    ALTER TABLE "ProductionCertificates" RENAME COLUMN "Period_DateTo" TO "DateTo";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230830104717_Unique') THEN
    ALTER TABLE "ProductionCertificates" RENAME COLUMN "Period_DateFrom" TO "DateFrom";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230830104717_Unique') THEN
    CREATE UNIQUE INDEX "IX_ProductionCertificates_Gsrn_DateFrom_DateTo" ON "ProductionCertificates" ("Gsrn", "DateFrom", "DateTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230830104717_Unique') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230830104717_Unique', '7.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230831171652_sync') THEN
    CREATE TABLE "SynchronizationPositions" (
        "GSRN" text NOT NULL,
        "SyncedTo" bigint NOT NULL,
        CONSTRAINT "PK_SynchronizationPositions" PRIMARY KEY ("GSRN")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230831171652_sync') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230831171652_sync', '7.0.10');
    END IF;
END $EF$;
COMMIT;

