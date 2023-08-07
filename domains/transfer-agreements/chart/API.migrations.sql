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
    ALTER TABLE "TransferAgreements" ADD "ReceiverReference" uuid NOT NULL;
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

