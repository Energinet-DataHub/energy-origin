﻿CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230515102933_InitialCreate') THEN
    CREATE TABLE "TransferAgreements" (
        "Id" uuid NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone NOT NULL,
        "ReceiverTin" integer NOT NULL,
        CONSTRAINT "PK_TransferAgreements" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230515102933_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230515102933_InitialCreate', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230526073317_AddTransferAgreementActorIdAndSenderId') THEN
    ALTER TABLE "TransferAgreements" ADD "ActorId" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230526073317_AddTransferAgreementActorIdAndSenderId') THEN
    ALTER TABLE "TransferAgreements" ADD "SenderId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230526073317_AddTransferAgreementActorIdAndSenderId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230526073317_AddTransferAgreementActorIdAndSenderId', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230529134229_AlterColumnReceiverTin') THEN
    ALTER TABLE "TransferAgreements" ALTER COLUMN "ReceiverTin" TYPE text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230529134229_AlterColumnReceiverTin') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230529134229_AlterColumnReceiverTin', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230614101345_AddTransferAgreementsSenderNameAndSenderTin') THEN
    ALTER TABLE "TransferAgreements" ADD "SenderName" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230614101345_AddTransferAgreementsSenderNameAndSenderTin') THEN
    ALTER TABLE "TransferAgreements" ADD "SenderTin" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230614101345_AddTransferAgreementsSenderNameAndSenderTin') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230614101345_AddTransferAgreementsSenderNameAndSenderTin', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230618103100_UpdateEndDateToNullable') THEN
    ALTER TABLE "TransferAgreements" ALTER COLUMN "EndDate" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230618103100_UpdateEndDateToNullable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230618103100_UpdateEndDateToNullable', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230704092115_AddTransferAgreementHistoryEntry') THEN
    ALTER TABLE "TransferAgreements" DROP COLUMN "ActorId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230704092115_AddTransferAgreementHistoryEntry') THEN
    CREATE TABLE "TransferAgreementHistoryEntries" (
        "Id" uuid NOT NULL,
        "TransferAgreementId" uuid NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone,
        "ActorId" text NOT NULL,
        "ActorName" text NOT NULL,
        "SenderId" uuid NOT NULL,
        "SenderName" text NOT NULL,
        "SenderTin" text NOT NULL,
        "ReceiverTin" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "AuditAction" text NOT NULL,
        CONSTRAINT "PK_TransferAgreementHistoryEntries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TransferAgreementHistoryEntries_TransferAgreements_Transfer~" FOREIGN KEY ("TransferAgreementId") REFERENCES "TransferAgreements" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230704092115_AddTransferAgreementHistoryEntry') THEN
    CREATE INDEX "IX_TransferAgreementHistoryEntries_TransferAgreementId" ON "TransferAgreementHistoryEntries" ("TransferAgreementId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230704092115_AddTransferAgreementHistoryEntry') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230704092115_AddTransferAgreementHistoryEntry', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230807080407_AddTransferAgreementReceiverReference') THEN
    ALTER TABLE "TransferAgreements" ADD "ReceiverReference" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230807080407_AddTransferAgreementReceiverReference') THEN
    ALTER TABLE "TransferAgreements" ALTER COLUMN "ReceiverReference" DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230807080407_AddTransferAgreementReceiverReference') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230807080407_AddTransferAgreementReceiverReference', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230829090644_AddInvitationsTable') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'con') THEN
            CREATE SCHEMA con;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230829090644_AddInvitationsTable') THEN
    CREATE TABLE con."Invitation" (
        "Id" uuid NOT NULL,
        "SenderCompanyId" uuid NOT NULL,
        "SenderCompanyTin" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (current_timestamp at time zone 'UTC'),
        CONSTRAINT "PK_Invitation" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230829090644_AddInvitationsTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230829090644_AddInvitationsTable', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230829124003_AddUniqueIndexAndTransferAgreementNumber') THEN
    ALTER TABLE "TransferAgreements" ADD "TransferAgreementNumber" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230829124003_AddUniqueIndexAndTransferAgreementNumber') THEN
    CREATE UNIQUE INDEX "IX_TransferAgreements_SenderId_TransferAgreementNumber" ON "TransferAgreements" ("SenderId", "TransferAgreementNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230829124003_AddUniqueIndexAndTransferAgreementNumber') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230829124003_AddUniqueIndexAndTransferAgreementNumber', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904142106_RemovedSchemaAndRenamedInvitationsAndAddedConnection') THEN
    DROP TABLE con."Invitation";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904142106_RemovedSchemaAndRenamedInvitationsAndAddedConnection') THEN
    CREATE TABLE "ConnectionInvitations" (
        "Id" uuid NOT NULL,
        "SenderCompanyId" uuid NOT NULL,
        "SenderCompanyTin" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (current_timestamp at time zone 'UTC'),
        CONSTRAINT "PK_ConnectionInvitations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904142106_RemovedSchemaAndRenamedInvitationsAndAddedConnection') THEN
    CREATE TABLE "Connections" (
        "Id" uuid NOT NULL,
        "CompanyAId" uuid NOT NULL,
        "CompanyATin" text NOT NULL,
        "CompanyBId" uuid NOT NULL,
        "CompanyBTin" text NOT NULL,
        CONSTRAINT "PK_Connections" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904142106_RemovedSchemaAndRenamedInvitationsAndAddedConnection') THEN
    CREATE INDEX "IX_Connections_CompanyAId" ON "Connections" ("CompanyAId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904142106_RemovedSchemaAndRenamedInvitationsAndAddedConnection') THEN
    CREATE INDEX "IX_Connections_CompanyBId" ON "Connections" ("CompanyBId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904142106_RemovedSchemaAndRenamedInvitationsAndAddedConnection') THEN
    DROP SCHEMA IF EXISTS con CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904142106_RemovedSchemaAndRenamedInvitationsAndAddedConnection') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230904142106_RemovedSchemaAndRenamedInvitationsAndAddedConnection', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231102084120_AddClaimAutomationArgument') THEN
    CREATE TABLE "ClaimAutomationArguments" (
        "SubjectId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ClaimAutomationArguments" PRIMARY KEY ("SubjectId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231102084120_AddClaimAutomationArgument') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20231102084120_AddClaimAutomationArgument', '8.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231123093303_AddTransferAgreementProposalsAndDeleteConnections') THEN
    DROP TABLE "ConnectionInvitations";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231123093303_AddTransferAgreementProposalsAndDeleteConnections') THEN
    DROP TABLE "Connections";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231123093303_AddTransferAgreementProposalsAndDeleteConnections') THEN
    CREATE TABLE "TransferAgreementProposals" (
        "Id" uuid NOT NULL,
        "SenderCompanyId" uuid NOT NULL,
        "SenderCompanyTin" text NOT NULL,
        "SenderCompanyName" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (current_timestamp at time zone 'UTC'),
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone,
        "ReceiverCompanyTin" text,
        CONSTRAINT "PK_TransferAgreementProposals" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20231123093303_AddTransferAgreementProposalsAndDeleteConnections') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20231123093303_AddTransferAgreementProposalsAndDeleteConnections', '8.0.1');
    END IF;
END $EF$;
COMMIT;

