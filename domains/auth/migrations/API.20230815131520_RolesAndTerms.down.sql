DROP TABLE "CompanyTerms";

DROP TABLE "UserRoles";

ALTER TABLE "Users" ADD "AcceptedTermsVersion" integer NOT NULL DEFAULT 0;

    UPDATE "Users"
    SET "AcceptedTermsVersion" = "UserTerms"."AcceptedVersion"
    FROM "UserTerms"
    WHERE "Users"."Id" = "UserTerms"."UserId";

DROP TABLE "UserTerms";

DROP TYPE company_terms_type;
DROP TYPE user_terms_type;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20230815131520_RolesAndTerms';

