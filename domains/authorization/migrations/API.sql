CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
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
    VALUES ('20240514093230_InitialCreate', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20240620115450_AddOrganizationTinUniqueIndex', '9.0.0');
    END IF;
END $EF$;

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
    VALUES ('20240711101829_AddTermsWithOutbox', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240730115826_AddDefaultTermsWithUniqueConstraint') THEN
    INSERT INTO "Terms" ("Id", "Version")
    VALUES ('0ccb0348-3179-4b96-9be0-dc7ab1541771', 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240730115826_AddDefaultTermsWithUniqueConstraint') THEN
    CREATE UNIQUE INDEX "IX_Terms_Version" ON "Terms" ("Version");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240730115826_AddDefaultTermsWithUniqueConstraint') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240730115826_AddDefaultTermsWithUniqueConstraint', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007173932_AddOrganizationConsent') THEN
    ALTER TABLE "Organizations" ALTER COLUMN "Tin" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007173932_AddOrganizationConsent') THEN
    ALTER TABLE "Clients" ADD "OrganizationId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007173932_AddOrganizationConsent') THEN
    CREATE TABLE "OrganizationConsents" (
        "Id" uuid NOT NULL,
        "ConsentGiverOrganizationId" uuid NOT NULL,
        "ConsentReceiverOrganizationId" uuid NOT NULL,
        "ConsentDate" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OrganizationConsents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrganizationConsents_Organizations_ConsentGiverOrganization~" FOREIGN KEY ("ConsentGiverOrganizationId") REFERENCES "Organizations" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_OrganizationConsents_Organizations_ConsentReceiverOrganizat~" FOREIGN KEY ("ConsentReceiverOrganizationId") REFERENCES "Organizations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007173932_AddOrganizationConsent') THEN
    CREATE INDEX "IX_Clients_OrganizationId" ON "Clients" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007173932_AddOrganizationConsent') THEN
    CREATE INDEX "IX_OrganizationConsents_ConsentGiverOrganizationId" ON "OrganizationConsents" ("ConsentGiverOrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007173932_AddOrganizationConsent') THEN
    CREATE INDEX "IX_OrganizationConsents_ConsentReceiverOrganizationId" ON "OrganizationConsents" ("ConsentReceiverOrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007173932_AddOrganizationConsent') THEN
    ALTER TABLE "Clients" ADD CONSTRAINT "FK_Clients_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007173932_AddOrganizationConsent') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20241007173932_AddOrganizationConsent', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007174608_AddOrganizationConsentInsertOrganizations') THEN
    DO $$
    BEGIN
       IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'uuid-ossp') THEN
          CREATE EXTENSION "uuid-ossp";
       END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007174608_AddOrganizationConsentInsertOrganizations') THEN
    WITH inserted_orgs AS (
        INSERT INTO public."Organizations" ("Id", "Tin", "Name", "TermsAcceptanceDate", "TermsAccepted", "TermsVersion")
        SELECT
            uuid_generate_v4() AS "Id",
            null AS "Tin",
            "Name" AS "Name",
            null AS "TermsAcceptanceDate",
            false AS "TermsAccepted",
            null AS "TermsVersion"
        FROM public."Clients"
        RETURNING "Id", "Name"
    )

    UPDATE
    	public."Clients" c
    SET
    	"OrganizationId" = i."Id"
    FROM
    	inserted_orgs i
    WHERE
    	c."Name" = i."Name";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007174608_AddOrganizationConsentInsertOrganizations') THEN
    INSERT INTO public."OrganizationConsents" ("Id", "ConsentGiverOrganizationId", "ConsentReceiverOrganizationId", "ConsentDate")
    SELECT
    	uuid_generate_v4() AS "Id",
    	con."OrganizationId" AS "ConsentGiverOrganizationId",
    	cli."OrganizationId" AS "ConsentReceiverOrganizationId",
    	con."ConsentDate" AS "ConsentDate"
    FROM
    	public."Consents" as con
    INNER JOIN
    	public."Clients" AS cli
    ON
    	con."ClientId" = cli."Id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241007174608_AddOrganizationConsentInsertOrganizations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20241007174608_AddOrganizationConsentInsertOrganizations', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241021074018_RemoveConsentTable') THEN
    DROP TABLE "Consents";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241021074018_RemoveConsentTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20241021074018_RemoveConsentTable', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241022085628_AddUniqueConsentIndex') THEN
    DROP INDEX "IX_OrganizationConsents_ConsentReceiverOrganizationId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241022085628_AddUniqueConsentIndex') THEN
    CREATE UNIQUE INDEX "IX_OrganizationConsents_ConsentReceiverOrganizationId_ConsentG~" ON "OrganizationConsents" ("ConsentReceiverOrganizationId", "ConsentGiverOrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20241022085628_AddUniqueConsentIndex') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20241022085628_AddUniqueConsentIndex', '9.0.0');
    END IF;
END $EF$;
COMMIT;

