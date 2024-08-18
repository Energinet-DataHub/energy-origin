DO $$
DECLARE
organizations_empty boolean;
    users_empty boolean;
    affiliations_empty boolean;
    terms_empty boolean;
BEGIN

SELECT COUNT(*) = 0 FROM "Terms" INTO terms_empty;
IF terms_empty THEN
        INSERT INTO "Terms" ("Id", "Version")
        VALUES ('0ccb0348-3179-4b96-9be0-dc7ab1541771', 1);

        RAISE NOTICE 'Terms table seeded successfully.';
ELSE
        RAISE NOTICE 'Terms table is not empty. Skipping seed data insertion.';
END IF;

SELECT COUNT(*) = 0 FROM "Organizations" INTO organizations_empty;
IF organizations_empty THEN
        INSERT INTO "Organizations" ("Id", "Tin", "Name", "TermsAcceptanceDate", "TermsAccepted", "TermsVersion")
        VALUES
            ('21e150bd-8a0b-4ba6-905b-7223db248af5', '66666666', 'Bolighaj', CURRENT_TIMESTAMP, TRUE, 1),
            ('64cf9c0f-c634-41c0-9cba-4394024b915f', '11223344', 'Producent A/S', CURRENT_TIMESTAMP, TRUE, 1),
            ('68237a9a-ff1c-48e1-83c5-d9857ef26587', '28980671', 'Energinet', CURRENT_TIMESTAMP, TRUE, 1),
            ('7f4b6c63-8fbd-4927-8202-c6731c00a0fc', '77777777', 'Viggos Vindmøller', CURRENT_TIMESTAMP, TRUE, 1),
            ('95d7be81-0cfb-4b52-9c92-33a45747fcef', '39293595', 'Company Inc.', CURRENT_TIMESTAMP, TRUE, 1),
            ('b0918bf8-626d-4b35-a0ad-3f5beb6b6fef', '55555555', 'Fabrikant', CURRENT_TIMESTAMP, TRUE, 1),
            ('cb1e0981-1ea7-404e-b189-260c58a46c68', '12345678', 'Startup I/S', CURRENT_TIMESTAMP, TRUE, 1);

        RAISE NOTICE 'Organizations table seeded successfully.';
ELSE
        RAISE NOTICE 'Organizations table is not empty. Skipping seed data insertion.';
END IF;

SELECT COUNT(*) = 0 FROM "Users" INTO users_empty;
IF users_empty THEN
        INSERT INTO "Users" ("Id", "IdpUserId", "Name")
        VALUES
            ('3dce58e3-293c-46c9-b010-57f558117812', '64cf9c0f-c634-41c0-9cba-4394024b915f', 'Peter Producent'),
            ('6c59efbf-9c41-45d8-a340-ccc85471c959', 'cb1e0981-1ea7-404e-b189-260c58a46c68', 'Ivan Iværksætter'),
            ('7cb03d9d-f8d6-4039-ace4-2664429c6be9', '68237a9a-ff1c-48e1-83c5-d9857ef26587', 'Erik Energinet'),
            ('b34f24f3-51a4-4a00-8d4b-1a212a47c791', '95d7be81-0cfb-4b52-9c92-33a45747fcef', 'Charlotte C.S. Rasmussen'),
            ('b3519236-aaf5-4eef-84b7-724dc8289fb2', '7f4b6c63-8fbd-4927-8202-c6731c00a0fc', 'Viggos Vindmøller'),
            ('df7bfcf7-c2c8-442b-83cc-b46e6e8c8e72', 'b0918bf8-626d-4b35-a0ad-3f5beb6b6fef', 'Fabrikant'),
            ('ec738dea-6903-490d-b012-9a2fe98406dc', '21e150bd-8a0b-4ba6-905b-7223db248af5', 'Bolighaj');

        RAISE NOTICE 'Users table seeded successfully.';
ELSE
        RAISE NOTICE 'Users table is not empty. Skipping seed data insertion.';
END IF;

SELECT COUNT(*) = 0 FROM "Affiliations" INTO affiliations_empty;
IF affiliations_empty THEN
        INSERT INTO "Affiliations" ("UserId", "OrganizationId")
        VALUES
            ('3dce58e3-293c-46c9-b010-57f558117812', '64cf9c0f-c634-41c0-9cba-4394024b915f'),
            ('6c59efbf-9c41-45d8-a340-ccc85471c959', 'cb1e0981-1ea7-404e-b189-260c58a46c68'),
            ('7cb03d9d-f8d6-4039-ace4-2664429c6be9', '68237a9a-ff1c-48e1-83c5-d9857ef26587'),
            ('b34f24f3-51a4-4a00-8d4b-1a212a47c791', '95d7be81-0cfb-4b52-9c92-33a45747fcef'),
            ('b3519236-aaf5-4eef-84b7-724dc8289fb2', '7f4b6c63-8fbd-4927-8202-c6731c00a0fc'),
            ('df7bfcf7-c2c8-442b-83cc-b46e6e8c8e72', 'b0918bf8-626d-4b35-a0ad-3f5beb6b6fef'),
            ('ec738dea-6903-490d-b012-9a2fe98406dc', '21e150bd-8a0b-4ba6-905b-7223db248af5');

        RAISE NOTICE 'Affiliations table seeded successfully.';
ELSE
        RAISE NOTICE 'Affiliations table is not empty. Skipping seed data insertion.';
END IF;
END $$;
