    CREATE TYPE provider_key_type_x AS ENUM ('pid', 'rid', 'mit_id_uuid');
    ALTER TABLE "UserProviders" ADD COLUMN "ProviderKeyTypeX" provider_key_type_x DEFAULT('pid');
    DELETE FROM "UserProviders" WHERE "ProviderKeyType" = 'eia';
    UPDATE "UserProviders" SET "ProviderKeyTypeX" = "ProviderKeyType"::text::provider_key_type_x;
    ALTER TABLE "UserProviders" ALTER COLUMN "ProviderKeyTypeX" DROP DEFAULT;
    ALTER TABLE "UserProviders" ALTER COLUMN "ProviderKeyTypeX" SET NOT NULL;
    ALTER TABLE "UserProviders" DROP COLUMN "ProviderKeyType";
    ALTER TABLE "UserProviders" RENAME COLUMN "ProviderKeyTypeX" TO "ProviderKeyType";
    DROP TYPE provider_key_type;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20230413105444_AddEiaProviderKeyTypeEnum';

