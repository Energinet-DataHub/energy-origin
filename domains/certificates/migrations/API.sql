CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230911185804_Initial') THEN
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
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230911185804_Initial') THEN
    CREATE TABLE "ProductionCertificates" (
        "Id" uuid NOT NULL,
        "IssuedState" integer NOT NULL,
        "GridArea" text NOT NULL,
        "DateFrom" bigint NOT NULL,
        "DateTo" bigint NOT NULL,
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
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230911185804_Initial') THEN
    CREATE TABLE "SynchronizationPositions" (
        "GSRN" text NOT NULL,
        "SyncedTo" bigint NOT NULL,
        CONSTRAINT "PK_SynchronizationPositions" PRIMARY KEY ("GSRN")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230911185804_Initial') THEN
    CREATE UNIQUE INDEX "IX_Contracts_GSRN_ContractNumber" ON "Contracts" ("GSRN", "ContractNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230911185804_Initial') THEN
    CREATE UNIQUE INDEX "IX_ProductionCertificates_Gsrn_DateFrom_DateTo" ON "ProductionCertificates" ("Gsrn", "DateFrom", "DateTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230911185804_Initial') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230911185804_Initial', '7.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231017070514_AddedConsumptionCertificates') THEN
    CREATE TABLE "ConsumptionCertificates" (
        "Id" uuid NOT NULL,
        "IssuedState" integer NOT NULL,
        "GridArea" text NOT NULL,
        "DateFrom" bigint NOT NULL,
        "DateTo" bigint NOT NULL,
        "MeteringPointOwner" text NOT NULL,
        "Gsrn" text NOT NULL,
        "Quantity" bigint NOT NULL,
        "BlindingValue" bytea NOT NULL,
        "RejectionReason" text NULL,
        CONSTRAINT "PK_ConsumptionCertificates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231017070514_AddedConsumptionCertificates') THEN
    CREATE UNIQUE INDEX "IX_ConsumptionCertificates_Gsrn_DateFrom_DateTo" ON "ConsumptionCertificates" ("Gsrn", "DateFrom", "DateTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231017070514_AddedConsumptionCertificates') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20231017070514_AddedConsumptionCertificates', '7.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231107095405_AddTechnologyToCertificateIssuingContract') THEN
    ALTER TABLE "Contracts" ADD "Technology_FuelCode" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231107095405_AddTechnologyToCertificateIssuingContract') THEN
    ALTER TABLE "Contracts" ADD "Technology_TechCode" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231107095405_AddTechnologyToCertificateIssuingContract') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20231107095405_AddTechnologyToCertificateIssuingContract', '7.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231114121811_UpdateEmptyTechnologyCodes') THEN

        UPDATE "Contracts"
        SET
            "Technology_FuelCode" = CASE
                WHEN "Technology_FuelCode" = '' THEN 'F00000000'
                ELSE "Technology_FuelCode"
            END,
            "Technology_TechCode" = CASE
                WHEN "Technology_TechCode" = '' THEN 'T070000'
                ELSE "Technology_TechCode"
            END
        WHERE "Technology_FuelCode" = '' OR "Technology_TechCode" = '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231114121811_UpdateEmptyTechnologyCodes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20231114121811_UpdateEmptyTechnologyCodes', '7.0.10');
    END IF;
END $EF$;
COMMIT;

