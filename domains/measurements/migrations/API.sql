CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240311094200_Addrelations') THEN
    CREATE TABLE "Relations" (
        "SubjectId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "Actor" uuid NOT NULL,
        "Tin" text,
        CONSTRAINT "PK_Relations" PRIMARY KEY ("SubjectId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240311094200_Addrelations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20240311094200_Addrelations', '9.0.0');
    END IF;
END $EF$;
COMMIT;

