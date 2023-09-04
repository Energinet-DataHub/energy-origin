CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
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
    VALUES ('20230515102933_InitialCreate', '7.0.5');
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
    VALUES ('20230526073317_AddTransferAgreementActorIdAndSenderId', '7.0.5');
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
    VALUES ('20230529134229_AlterColumnReceiverTin', '7.0.5');
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
    VALUES ('20230614101345_AddTransferAgreementsSenderNameAndSenderTin', '7.0.5');
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
    VALUES ('20230618103100_UpdateEndDateToNullable', '7.0.5');
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
        "EndDate" timestamp with time zone NULL,
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
    VALUES ('20230704092115_AddTransferAgreementHistoryEntry', '7.0.5');
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
    VALUES ('20230807080407_AddTransferAgreementReceiverReference', '7.0.5');
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
    VALUES ('20230829090644_AddInvitationsTable', '7.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230829103346_AddConnectionsTable') THEN
    CREATE TABLE con."Connection" (
        "Id" uuid NOT NULL,
        "CompanyAId" uuid NOT NULL,
        "CompanyATin" text NOT NULL,
        "CompanyBId" uuid NOT NULL,
        "CompanyBTin" text NOT NULL,
        CONSTRAINT "PK_Connection" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230829103346_AddConnectionsTable') THEN
    CREATE INDEX "IX_Connection_CompanyAId" ON con."Connection" ("CompanyAId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230829103346_AddConnectionsTable') THEN
    CREATE INDEX "IX_Connection_CompanyBId" ON con."Connection" ("CompanyBId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230829103346_AddConnectionsTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230829103346_AddConnectionsTable', '7.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904130847_RemovedSchemaAndRenamedInvitations') THEN
    DROP TABLE con."Invitation";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904130847_RemovedSchemaAndRenamedInvitations') THEN
    ALTER TABLE con."Connection" DROP CONSTRAINT "PK_Connection";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904130847_RemovedSchemaAndRenamedInvitations') THEN
    ALTER TABLE con."Connection" RENAME TO "Connections";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904130847_RemovedSchemaAndRenamedInvitations') THEN
    ALTER TABLE "Connections" ADD CONSTRAINT "PK_Connections" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904130847_RemovedSchemaAndRenamedInvitations') THEN
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
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230904130847_RemovedSchemaAndRenamedInvitations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230904130847_RemovedSchemaAndRenamedInvitations', '7.0.5');
    END IF;
END $EF$;
COMMIT;

