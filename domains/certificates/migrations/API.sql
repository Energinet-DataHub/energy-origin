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
        "EndDate" timestamp with time zone,
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
        "RejectionReason" text,
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
    VALUES ('20230911185804_Initial', '8.0.6');
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
        "RejectionReason" text,
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
    VALUES ('20231017070514_AddedConsumptionCertificates', '8.0.6');
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
    VALUES ('20231107095405_AddTechnologyToCertificateIssuingContract', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231115153157_RemoveTechnologyColumnsFromContracts') THEN
    ALTER TABLE "Contracts" DROP COLUMN "Technology_FuelCode";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231115153157_RemoveTechnologyColumnsFromContracts') THEN
    ALTER TABLE "Contracts" DROP COLUMN "Technology_TechCode";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231115153157_RemoveTechnologyColumnsFromContracts') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20231115153157_RemoveTechnologyColumnsFromContracts', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231115153942_AddTechnologyToContractsTable') THEN
    ALTER TABLE "Contracts" ADD "Technology_FuelCode" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231115153942_AddTechnologyToContractsTable') THEN
    ALTER TABLE "Contracts" ADD "Technology_TechCode" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231115153942_AddTechnologyToContractsTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20231115153942_AddTechnologyToContractsTable', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231115155411_UpdateNullTechnologyCodes') THEN

        UPDATE "Contracts"
        SET
            "Technology_FuelCode" = 'F00000000',
            "Technology_TechCode" = 'T070000'
        WHERE "Technology_FuelCode" IS NULL
          AND "Technology_TechCode" IS NULL
          AND "MeteringPointType" = 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231115155411_UpdateNullTechnologyCodes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20231115155411_UpdateNullTechnologyCodes', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231220131700_AddOutbox') THEN
    CREATE TABLE "InboxState" (
        "Id" bigint GENERATED BY DEFAULT AS IDENTITY,
        "MessageId" uuid NOT NULL,
        "ConsumerId" uuid NOT NULL,
        "LockId" uuid NOT NULL,
        "RowVersion" bytea,
        "Received" timestamp with time zone NOT NULL,
        "ReceiveCount" integer NOT NULL,
        "ExpirationTime" timestamp with time zone,
        "Consumed" timestamp with time zone,
        "Delivered" timestamp with time zone,
        "LastSequenceNumber" bigint,
        CONSTRAINT "PK_InboxState" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_InboxState_MessageId_ConsumerId" UNIQUE ("MessageId", "ConsumerId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231220131700_AddOutbox') THEN
    CREATE TABLE "OutboxMessage" (
        "SequenceNumber" bigint GENERATED BY DEFAULT AS IDENTITY,
        "EnqueueTime" timestamp with time zone,
        "SentTime" timestamp with time zone NOT NULL,
        "Headers" text,
        "Properties" text,
        "InboxMessageId" uuid,
        "InboxConsumerId" uuid,
        "OutboxId" uuid,
        "MessageId" uuid NOT NULL,
        "ContentType" character varying(256) NOT NULL,
        "Body" text NOT NULL,
        "ConversationId" uuid,
        "CorrelationId" uuid,
        "InitiatorId" uuid,
        "RequestId" uuid,
        "SourceAddress" character varying(256),
        "DestinationAddress" character varying(256),
        "ResponseAddress" character varying(256),
        "FaultAddress" character varying(256),
        "ExpirationTime" timestamp with time zone,
        CONSTRAINT "PK_OutboxMessage" PRIMARY KEY ("SequenceNumber")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231220131700_AddOutbox') THEN
    CREATE TABLE "OutboxState" (
        "OutboxId" uuid NOT NULL,
        "LockId" uuid NOT NULL,
        "RowVersion" bytea,
        "Created" timestamp with time zone NOT NULL,
        "Delivered" timestamp with time zone,
        "LastSequenceNumber" bigint,
        CONSTRAINT "PK_OutboxState" PRIMARY KEY ("OutboxId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231220131700_AddOutbox') THEN
    CREATE INDEX "IX_InboxState_Delivered" ON "InboxState" ("Delivered");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231220131700_AddOutbox') THEN
    CREATE INDEX "IX_OutboxMessage_EnqueueTime" ON "OutboxMessage" ("EnqueueTime");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231220131700_AddOutbox') THEN
    CREATE INDEX "IX_OutboxMessage_ExpirationTime" ON "OutboxMessage" ("ExpirationTime");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231220131700_AddOutbox') THEN
    CREATE UNIQUE INDEX "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber" ON "OutboxMessage" ("InboxMessageId", "InboxConsumerId", "SequenceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231220131700_AddOutbox') THEN
    CREATE UNIQUE INDEX "IX_OutboxMessage_OutboxId_SequenceNumber" ON "OutboxMessage" ("OutboxId", "SequenceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231220131700_AddOutbox') THEN
    CREATE INDEX "IX_OutboxState_Created" ON "OutboxState" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231220131700_AddOutbox') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20231220131700_AddOutbox', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240122103441_UpdateToOutbox') THEN
    ALTER TABLE "OutboxMessage" ADD "MessageType" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240122103441_UpdateToOutbox') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240122103441_UpdateToOutbox', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240125131645_AddActivityLog') THEN
    CREATE TABLE "ActivityLogs" (
        "Id" uuid NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "ActorId" uuid NOT NULL,
        "ActorType" integer NOT NULL,
        "ActorName" text NOT NULL,
        "OrganizationTin" text NOT NULL,
        "OrganizationName" text NOT NULL,
        "EntityType" integer NOT NULL,
        "ActionType" integer NOT NULL,
        "EntityId" uuid NOT NULL,
        CONSTRAINT "PK_ActivityLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240125131645_AddActivityLog') THEN
    CREATE INDEX "IX_ActivityLogs_OrganizationTin" ON "ActivityLogs" ("OrganizationTin");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240125131645_AddActivityLog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240125131645_AddActivityLog', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240216110232_ActivityLogEntityIdIsNowAString') THEN
    ALTER TABLE "ActivityLogs" ALTER COLUMN "EntityId" TYPE text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240216110232_ActivityLogEntityIdIsNowAString') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240216110232_ActivityLogEntityIdIsNowAString', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240306103908_AddOtherOrgToActivityLog') THEN
    ALTER TABLE "ActivityLogs" ADD "OtherOrganizationName" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240306103908_AddOtherOrgToActivityLog') THEN
    ALTER TABLE "ActivityLogs" ADD "OtherOrganizationTin" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240306103908_AddOtherOrgToActivityLog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240306103908_AddOtherOrgToActivityLog', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240408104920_AddSlidingWindow') THEN
    CREATE TABLE "MeteringPointTimeSeriesSlidingWindows" (
        "GSRN" text NOT NULL,
        "SynchronizationPoint" bigint NOT NULL,
        "MissingMeasurements" jsonb NOT NULL,
        CONSTRAINT "PK_MeteringPointTimeSeriesSlidingWindows" PRIMARY KEY ("GSRN")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240408104920_AddSlidingWindow') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240408104920_AddSlidingWindow', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240423100351_ModifyWalletUrlsOnContracts') THEN
    UPDATE public."Contracts" SET "WalletUrl" = REPLACE("WalletUrl", '/wallet-api', '/v1/slices');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240423100351_ModifyWalletUrlsOnContracts') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240423100351_ModifyWalletUrlsOnContracts', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240503112223_ModifyWalletUrlsOnContractsToContainWalletApi') THEN
    UPDATE public."Contracts" SET "WalletUrl" = REPLACE("WalletUrl", '/v1/slices', '/wallet-api/v1/slices');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240503112223_ModifyWalletUrlsOnContractsToContainWalletApi') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240503112223_ModifyWalletUrlsOnContractsToContainWalletApi', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240701111541_RemovedSyncPosition') THEN
    DROP TABLE "SynchronizationPositions";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240701111541_RemovedSyncPosition') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240701111541_RemovedSyncPosition', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240723091045_ModifiedContractsToUseRecipientIdAndRemovedCertificates') THEN
    DELETE FROM public."Contracts";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240723091045_ModifiedContractsToUseRecipientIdAndRemovedCertificates') THEN
    DROP TABLE "ConsumptionCertificates";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240723091045_ModifiedContractsToUseRecipientIdAndRemovedCertificates') THEN
    DROP TABLE "ProductionCertificates";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240723091045_ModifiedContractsToUseRecipientIdAndRemovedCertificates') THEN
    ALTER TABLE "Contracts" DROP COLUMN "WalletPublicKey";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240723091045_ModifiedContractsToUseRecipientIdAndRemovedCertificates') THEN
    ALTER TABLE "Contracts" DROP COLUMN "WalletUrl";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240723091045_ModifiedContractsToUseRecipientIdAndRemovedCertificates') THEN
    ALTER TABLE "Contracts" ADD "RecipientId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240723091045_ModifiedContractsToUseRecipientIdAndRemovedCertificates') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240723091045_ModifiedContractsToUseRecipientIdAndRemovedCertificates', '8.0.6');
    END IF;
END $EF$;
COMMIT;

