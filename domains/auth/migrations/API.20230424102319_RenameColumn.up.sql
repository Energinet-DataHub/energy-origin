ALTER TABLE "Users" RENAME COLUMN "AllowCPRLookup" TO "AllowCprLookup";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230424102319_RenameColumn', '7.0.3');

