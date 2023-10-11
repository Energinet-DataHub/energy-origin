    ALTER TYPE provider_key_type ADD VALUE 'eia' BEFORE 'mit_id_uuid';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230413105444_AddEiaProviderKeyTypeEnum', '7.0.3');

