CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "TransferAgreements" (
    "Id" uuid NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "ReceiverTin" integer NOT NULL,
    CONSTRAINT "PK_TransferAgreements" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230515102933_InitialCreate', '9.0.0');

ALTER TABLE "TransferAgreements" ADD "ActorId" text NOT NULL DEFAULT '';

ALTER TABLE "TransferAgreements" ADD "SenderId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230526073317_AddTransferAgreementActorIdAndSenderId', '9.0.0');

ALTER TABLE "TransferAgreements" ALTER COLUMN "ReceiverTin" TYPE text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230529134229_AlterColumnReceiverTin', '9.0.0');

ALTER TABLE "TransferAgreements" ADD "SenderName" text NOT NULL DEFAULT '';

ALTER TABLE "TransferAgreements" ADD "SenderTin" text NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230614101345_AddTransferAgreementsSenderNameAndSenderTin', '9.0.0');

ALTER TABLE "TransferAgreements" ALTER COLUMN "EndDate" DROP NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230618103100_UpdateEndDateToNullable', '9.0.0');

ALTER TABLE "TransferAgreements" DROP COLUMN "ActorId";

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

CREATE INDEX "IX_TransferAgreementHistoryEntries_TransferAgreementId" ON "TransferAgreementHistoryEntries" ("TransferAgreementId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230704092115_AddTransferAgreementHistoryEntry', '9.0.0');

ALTER TABLE "TransferAgreements" ADD "ReceiverReference" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "TransferAgreements" ALTER COLUMN "ReceiverReference" DROP DEFAULT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230807080407_AddTransferAgreementReceiverReference', '9.0.0');

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'con') THEN
        CREATE SCHEMA con;
    END IF;
END $EF$;

CREATE TABLE con."Invitation" (
    "Id" uuid NOT NULL,
    "SenderCompanyId" uuid NOT NULL,
    "SenderCompanyTin" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (current_timestamp at time zone 'UTC'),
    CONSTRAINT "PK_Invitation" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230829090644_AddInvitationsTable', '9.0.0');

ALTER TABLE "TransferAgreements" ADD "TransferAgreementNumber" integer NOT NULL DEFAULT 0;

CREATE UNIQUE INDEX "IX_TransferAgreements_SenderId_TransferAgreementNumber" ON "TransferAgreements" ("SenderId", "TransferAgreementNumber");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230829124003_AddUniqueIndexAndTransferAgreementNumber', '9.0.0');

DROP TABLE con."Invitation";

CREATE TABLE "ConnectionInvitations" (
    "Id" uuid NOT NULL,
    "SenderCompanyId" uuid NOT NULL,
    "SenderCompanyTin" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (current_timestamp at time zone 'UTC'),
    CONSTRAINT "PK_ConnectionInvitations" PRIMARY KEY ("Id")
);

CREATE TABLE "Connections" (
    "Id" uuid NOT NULL,
    "CompanyAId" uuid NOT NULL,
    "CompanyATin" text NOT NULL,
    "CompanyBId" uuid NOT NULL,
    "CompanyBTin" text NOT NULL,
    CONSTRAINT "PK_Connections" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_Connections_CompanyAId" ON "Connections" ("CompanyAId");

CREATE INDEX "IX_Connections_CompanyBId" ON "Connections" ("CompanyBId");

DROP SCHEMA IF EXISTS con CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230904142106_RemovedSchemaAndRenamedInvitationsAndAddedConnection', '9.0.0');

CREATE TABLE "ClaimAutomationArguments" (
    "SubjectId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ClaimAutomationArguments" PRIMARY KEY ("SubjectId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231102084120_AddClaimAutomationArgument', '9.0.0');

DROP TABLE "ConnectionInvitations";

DROP TABLE "Connections";

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

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231123093303_AddTransferAgreementProposalsAndDeleteConnections', '9.0.0');

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

CREATE INDEX "IX_ActivityLogs_OrganizationTin" ON "ActivityLogs" ("OrganizationTin");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240125123642_AddActivitylogEntity', '9.0.0');

ALTER TABLE "ActivityLogs" ALTER COLUMN "EntityId" TYPE text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240216131219_ActivityLogEntityIdIsNowAString', '9.0.0');

ALTER TABLE "ActivityLogs" ADD "OtherOrganizationName" text NOT NULL DEFAULT '';

ALTER TABLE "ActivityLogs" ADD "OtherOrganizationTin" text NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240305123915_AddOtherOrganizationToActivityLogEntries', '9.0.0');

ALTER TABLE "TransferAgreements" ADD "ReceiverName" text NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240307132055_AddReceiverNameToTransferAgreements', '9.0.0');

DROP TABLE "TransferAgreementHistoryEntries";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240409063522_RemoveTransferAgreementHistory', '9.0.0');

ALTER TABLE "TransferAgreements" ADD "Type" integer NOT NULL DEFAULT 0;

ALTER TABLE "TransferAgreementProposals" ADD "Type" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241107113909_AddTransferAgreementType', '9.0.0');

COMMIT;

