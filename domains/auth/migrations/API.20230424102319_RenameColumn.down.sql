ALTER TABLE "Users" RENAME COLUMN "AllowCprLookup" TO "AllowCPRLookup";

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20230424102319_RenameColumn';

