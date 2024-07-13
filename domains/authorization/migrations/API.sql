﻿CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    CREATE TABLE "Clients" (
        "Id" uuid NOT NULL,
        "IdpClientId" uuid NOT NULL,
        "Name" text NOT NULL,
        "ClientType" integer NOT NULL,
        "RedirectUrl" text NOT NULL,
        CONSTRAINT "PK_Clients" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    CREATE TABLE "Organizations" (
        "Id" uuid NOT NULL,
        "Tin" text NOT NULL,
        "Name" text NOT NULL,
        CONSTRAINT "PK_Organizations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "IdpUserId" uuid NOT NULL,
        "Name" text NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    CREATE TABLE "Consents" (
        "OrganizationId" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "ConsentDate" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Consents" PRIMARY KEY ("ClientId", "OrganizationId"),
        CONSTRAINT "FK_Consents_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Consents_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    CREATE TABLE "Affiliations" (
        "UserId" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        CONSTRAINT "PK_Affiliations" PRIMARY KEY ("UserId", "OrganizationId"),
        CONSTRAINT "FK_Affiliations_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Affiliations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    CREATE INDEX "IX_Affiliations_OrganizationId" ON "Affiliations" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Clients_IdpClientId" ON "Clients" ("IdpClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Consents_ClientId_OrganizationId" ON "Consents" ("ClientId", "OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    CREATE INDEX "IX_Consents_OrganizationId" ON "Consents" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_IdpUserId" ON "Users" ("IdpUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240514093230_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240514093230_InitialCreate', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240620115450_AddOrganizationTinUniqueIndex') THEN
    CREATE UNIQUE INDEX "IX_Organizations_Tin" ON "Organizations" ("Tin");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240620115450_AddOrganizationTinUniqueIndex') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240620115450_AddOrganizationTinUniqueIndex', '8.0.6');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    ALTER TABLE "Organizations" ADD "TermsAcceptanceDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    ALTER TABLE "Organizations" ADD "TermsAccepted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    ALTER TABLE "Organizations" ADD "TermsVersion" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
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
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
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
        "MessageType" text NOT NULL,
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
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
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
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    CREATE TABLE "Terms" (
        "Id" uuid NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_Terms" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    CREATE INDEX "IX_InboxState_Delivered" ON "InboxState" ("Delivered");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    CREATE INDEX "IX_OutboxMessage_EnqueueTime" ON "OutboxMessage" ("EnqueueTime");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    CREATE INDEX "IX_OutboxMessage_ExpirationTime" ON "OutboxMessage" ("ExpirationTime");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    CREATE UNIQUE INDEX "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber" ON "OutboxMessage" ("InboxMessageId", "InboxConsumerId", "SequenceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    CREATE UNIQUE INDEX "IX_OutboxMessage_OutboxId_SequenceNumber" ON "OutboxMessage" ("OutboxId", "SequenceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    CREATE INDEX "IX_OutboxState_Created" ON "OutboxState" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240711101829_AddTermsWithOutbox') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240711101829_AddTermsWithOutbox', '8.0.6');
    END IF;
END $EF$;
COMMIT;

