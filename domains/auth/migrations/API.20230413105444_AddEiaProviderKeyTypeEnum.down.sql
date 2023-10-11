    DELETE FROM "UserProviders" WHERE "ProviderKeyType" = 'eia';
    ALTER TABLE "UserProviders" ALTER COLUMN "ProviderKeyType" TYPE varchar(255);
    DROP TYPE provider_key_type;
    CREATE TYPE provider_key_type AS ENUM ('pid', 'rid', 'mit_id_uuid');
    ALTER TABLE "UserProviders" ALTER COLUMN "ProviderKeyType" TYPE provider_key_type USING "ProviderKeyType"::provider_key_type;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20230413105444_AddEiaProviderKeyTypeEnum';

