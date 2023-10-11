CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "ProviderId" text NOT NULL,
    "Name" text NOT NULL,
    "AcceptedTermsVersion" integer NOT NULL,
    "Tin" text NULL,
    "AllowCPRLookup" boolean NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_Users_ProviderId" ON "Users" ("ProviderId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230203091258_AddUserTable', '7.0.3');

