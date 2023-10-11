CREATE TYPE company_terms_type AS ENUM ('terms_of_service');
CREATE TYPE user_terms_type AS ENUM ('privacy_policy');

CREATE TABLE "CompanyTerms" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Type" company_terms_type NOT NULL,
    "AcceptedVersion" integer NOT NULL,
    CONSTRAINT "PK_CompanyTerms" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CompanyTerms_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "UserRoles" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" text NOT NULL,
    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "UserTerms" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Type" user_terms_type NOT NULL,
    "AcceptedVersion" integer NOT NULL,
    CONSTRAINT "PK_UserTerms" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserTerms_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

    INSERT INTO "UserTerms" ("Id", "UserId", "AcceptedVersion", "Type")
    SELECT gen_random_uuid(), "Id", "AcceptedTermsVersion", 'privacy_policy' FROM "Users";

CREATE UNIQUE INDEX "IX_CompanyTerms_CompanyId_Type" ON "CompanyTerms" ("CompanyId", "Type");

CREATE UNIQUE INDEX "IX_UserRoles_UserId_Role" ON "UserRoles" ("UserId", "Role");

CREATE UNIQUE INDEX "IX_UserTerms_UserId_Type" ON "UserTerms" ("UserId", "Type");

ALTER TABLE "Users" DROP COLUMN "AcceptedTermsVersion";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230815131520_RolesAndTerms', '7.0.3');

