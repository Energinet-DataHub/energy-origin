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
    VALUES ('20230515102933_InitialCreate', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20230526073317_AddTransferAgreementActorIdAndSenderId', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20230529134229_AlterColumnReceiverTin', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20230614101345_AddTransferAgreementsSenderNameAndSenderTin', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20230618103100_UpdateEndDateToNullable', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20230704092115_AddTransferAgreementHistoryEntry', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20230807080407_AddTransferAgreementReceiverReference', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20230829090644_AddInvitationsTable', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20230829124003_AddUniqueIndexAndTransferAgreementNumber', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20230904142106_RemovedSchemaAndRenamedInvitationsAndAddedConnection', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20231102084120_AddClaimAutomationArgument', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20231123093303_AddTransferAgreementProposalsAndDeleteConnections', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240125123642_AddActivitylogEntity') THEN
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
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240125123642_AddActivitylogEntity') THEN
    CREATE INDEX "IX_ActivityLogs_OrganizationTin" ON "ActivityLogs" ("OrganizationTin");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240125123642_AddActivitylogEntity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240125123642_AddActivitylogEntity', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240216131219_ActivityLogEntityIdIsNowAString') THEN
    ALTER TABLE "ActivityLogs" ALTER COLUMN "EntityId" TYPE text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240216131219_ActivityLogEntityIdIsNowAString') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240216131219_ActivityLogEntityIdIsNowAString', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240305123915_AddOtherOrganizationToActivityLogEntries') THEN
    ALTER TABLE "ActivityLogs" ADD "OtherOrganizationName" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240305123915_AddOtherOrganizationToActivityLogEntries') THEN
    ALTER TABLE "ActivityLogs" ADD "OtherOrganizationTin" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240305123915_AddOtherOrganizationToActivityLogEntries') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240305123915_AddOtherOrganizationToActivityLogEntries', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240307132055_AddReceiverNameToTransferAgreements') THEN
    ALTER TABLE "TransferAgreements" ADD "ReceiverName" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240307132055_AddReceiverNameToTransferAgreements') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240307132055_AddReceiverNameToTransferAgreements', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240409063522_RemoveTransferAgreementHistory') THEN
    DROP TABLE "TransferAgreementHistoryEntries";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240409063522_RemoveTransferAgreementHistory') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240409063522_RemoveTransferAgreementHistory', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241107113909_AddTransferAgreementType') THEN
    ALTER TABLE "TransferAgreements" ADD "Type" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241107113909_AddTransferAgreementType') THEN
    ALTER TABLE "TransferAgreementProposals" ADD "Type" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241107113909_AddTransferAgreementType') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20241107113909_AddTransferAgreementType', '9.0.0');
    END IF;
END $EF$;
COMMIT;

