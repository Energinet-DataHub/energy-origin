DO $$
DECLARE
companies_empty boolean;
    users_empty boolean;
    user_providers_empty boolean;
    user_terms_empty boolean;
BEGIN

SELECT COUNT(*) = 0 FROM "Companies" INTO companies_empty;
IF companies_empty THEN
        INSERT INTO "Companies" ("Id", "Name", "Tin") VALUES
            ('64cf9c0f-c634-41c0-9cba-4394024b915f', 'Producent A/S', '11223344'),
            ('b0918bf8-626d-4b35-a0ad-3f5beb6b6fef', 'Fabrikant', '55555555'),
            ('95d7be81-0cfb-4b52-9c92-33a45747fcef', 'Company Inc.', '39293595'),
            ('7f4b6c63-8fbd-4927-8202-c6731c00a0fc', 'Viggos Vindmøller', '77777777'),
            ('68237a9a-ff1c-48e1-83c5-d9857ef26587', 'Energinet', '28980671'),
            ('21e150bd-8a0b-4ba6-905b-7223db248af5', 'Bolighaj', '66666666'),
            ('cb1e0981-1ea7-404e-b189-260c58a46c68', 'Startup I/S', '12345678');
        RAISE NOTICE 'Companies table seeded successfully.';
ELSE
        RAISE NOTICE 'Companies table is not empty. Skipping seed data insertion.';
END IF;

SELECT COUNT(*) = 0 FROM "Users" INTO users_empty;
IF users_empty THEN
        INSERT INTO "Users" ("Id", "Name", "AllowCprLookup", "CompanyId") VALUES
            ('77ddb2a1-0dfc-48c1-b2b4-b66a59ba5223', 'Fabrikant', false, 'b0918bf8-626d-4b35-a0ad-3f5beb6b6fef'),
            ('3d13a6a9-3734-4301-b4fc-51d7c34c68d6', 'Erik Energinet', false, '68237a9a-ff1c-48e1-83c5-d9857ef26587'),
            ('a08c2894-7b18-4cf3-8731-610d7637a98f', 'Peter Producent', false, '64cf9c0f-c634-41c0-9cba-4394024b915f'),
            ('088204ae-9782-4ecb-97b2-3829c08ba7b8', 'Charlotte C.S. Rasmussen', false, '95d7be81-0cfb-4b52-9c92-33a45747fcef');
        RAISE NOTICE 'Users table seeded successfully.';
ELSE
        RAISE NOTICE 'Users table is not empty. Skipping seed data insertion.';
END IF;

SELECT COUNT(*) = 0 FROM "UserProviders" INTO user_providers_empty;
IF user_providers_empty THEN
        INSERT INTO "UserProviders" ("Id", "ProviderKeyType", "UserProviderKey", "UserId") VALUES
            ('53938ef0-a251-4953-a4e7-ebf921cff20f', 'rid', 'CVR:55555555-RID:987654321', '77ddb2a1-0dfc-48c1-b2b4-b66a59ba5223'),
            ('423c33ee-3ec0-4f04-ab2a-342526f6a3b3', 'rid', 'CVR:28980671-RID:987654321', '3d13a6a9-3734-4301-b4fc-51d7c34c68d6'),
            ('7a25578f-d957-49bf-bf5b-ffa7f6538671', 'rid', 'CVR:11223344-RID:987654321', 'a08c2894-7b18-4cf3-8731-610d7637a98f'),
            ('19391768-17a3-4ec6-b038-38110e08f938', 'rid', 'CVR:39293595-RID:987654321', '088204ae-9782-4ecb-97b2-3829c08ba7b8');
        RAISE NOTICE 'UserProviders table seeded successfully.';
ELSE
        RAISE NOTICE 'UserProviders table is not empty. Skipping seed data insertion.';
END IF;

SELECT COUNT(*) = 0 FROM "UserTerms" INTO user_terms_empty;
IF user_terms_empty THEN
        INSERT INTO "UserTerms" ("Id", "UserId", "Type", "AcceptedVersion") VALUES
            ('8f042d7c-e58f-442e-832f-1ab48640ebf3', '77ddb2a1-0dfc-48c1-b2b4-b66a59ba5223', 'privacy_policy', 2),
            ('dbcceb1d-bcbf-4b27-99cb-8fa7f85b3d79', '3d13a6a9-3734-4301-b4fc-51d7c34c68d6', 'privacy_policy', 2),
            ('cad3dcd7-eb36-4162-a4fc-c6100f3a9379', 'a08c2894-7b18-4cf3-8731-610d7637a98f', 'privacy_policy', 2),
            ('c03ec01f-fa63-473f-8b9c-547cd5f0d5de', '088204ae-9782-4ecb-97b2-3829c08ba7b8', 'privacy_policy', 2);
        RAISE NOTICE 'UserTerms table seeded successfully.';
ELSE
        RAISE NOTICE 'UserTerms table is not empty. Skipping seed data insertion.';
END IF;
END $$;
