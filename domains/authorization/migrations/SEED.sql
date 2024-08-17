BEGIN;

CREATE OR REPLACE FUNCTION upsert_organization(
    p_id UUID,
    p_tin VARCHAR(20),
    p_name VARCHAR(255),
    p_terms_acceptance_date TIMESTAMPTZ,
    p_terms_accepted BOOLEAN,
    p_terms_version INTEGER
) RETURNS VOID AS $$
BEGIN
INSERT INTO "Organizations" ("Id", "Tin", "Name", "TermsAcceptanceDate", "TermsAccepted", "TermsVersion")
VALUES (p_id, p_tin, p_name, p_terms_acceptance_date, p_terms_accepted, p_terms_version)
    ON CONFLICT ("Id") DO UPDATE SET
    "Tin" = EXCLUDED."Tin",
                              "Name" = EXCLUDED."Name",
                              "TermsAcceptanceDate" = EXCLUDED."TermsAcceptanceDate",
                              "TermsAccepted" = EXCLUDED."TermsAccepted",
                              "TermsVersion" = EXCLUDED."TermsVersion";
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION upsert_user(
    p_id UUID,
    p_idp_user_id UUID,
    p_name VARCHAR(255)
) RETURNS VOID AS $$
BEGIN
INSERT INTO "Users" ("Id", "IdpUserId", "Name")
VALUES (p_id, p_idp_user_id, p_name)
    ON CONFLICT ("Id") DO UPDATE SET
    "IdpUserId" = EXCLUDED."IdpUserId",
                              "Name" = EXCLUDED."Name";
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION upsert_affiliation(
    p_user_id UUID,
    p_organization_id UUID
) RETURNS VOID AS $$
BEGIN
INSERT INTO "Affiliations" ("UserId", "OrganizationId")
VALUES (p_user_id, p_organization_id)
    ON CONFLICT ("UserId", "OrganizationId") DO NOTHING;
END;
$$ LANGUAGE plpgsql;

SELECT upsert_organization('21e150bd-8a0b-4ba6-905b-7223db248af5', '66666666', 'Bolighaj', CURRENT_TIMESTAMP, TRUE, 1);
SELECT upsert_organization('64cf9c0f-c634-41c0-9cba-4394024b915f', '11223344', 'Producent A/S', CURRENT_TIMESTAMP, TRUE, 1);
SELECT upsert_organization('68237a9a-ff1c-48e1-83c5-d9857ef26587', '28980671', 'Energinet', CURRENT_TIMESTAMP, TRUE, 1);
SELECT upsert_organization('7f4b6c63-8fbd-4927-8202-c6731c00a0fc', '77777777', 'Viggos Vindmøller', CURRENT_TIMESTAMP, TRUE, 1);
SELECT upsert_organization('95d7be81-0cfb-4b52-9c92-33a45747fcef', '39293595', 'Company Inc.', CURRENT_TIMESTAMP, TRUE, 1);
SELECT upsert_organization('b0918bf8-626d-4b35-a0ad-3f5beb6b6fef', '55555555', 'Fabrikant', CURRENT_TIMESTAMP, TRUE, 1);
SELECT upsert_organization('cb1e0981-1ea7-404e-b189-260c58a46c68', '12345678', 'Startup I/S', CURRENT_TIMESTAMP, TRUE, 1);

SELECT upsert_user('3dce58e3-293c-46c9-b010-57f558117812', '64cf9c0f-c634-41c0-9cba-4394024b915f', 'Peter Producent');
SELECT upsert_affiliation('3dce58e3-293c-46c9-b010-57f558117812', '64cf9c0f-c634-41c0-9cba-4394024b915f');

SELECT upsert_user('6c59efbf-9c41-45d8-a340-ccc85471c959', 'cb1e0981-1ea7-404e-b189-260c58a46c68', 'Ivan Iværksætter');
SELECT upsert_affiliation('6c59efbf-9c41-45d8-a340-ccc85471c959', 'cb1e0981-1ea7-404e-b189-260c58a46c68');

SELECT upsert_user('7cb03d9d-f8d6-4039-ace4-2664429c6be9', '68237a9a-ff1c-48e1-83c5-d9857ef26587', 'Erik Energinet');
SELECT upsert_affiliation('7cb03d9d-f8d6-4039-ace4-2664429c6be9', '68237a9a-ff1c-48e1-83c5-d9857ef26587');

SELECT upsert_user('b34f24f3-51a4-4a00-8d4b-1a212a47c791', '95d7be81-0cfb-4b52-9c92-33a45747fcef', 'Charlotte C.S. Rasmussen');
SELECT upsert_affiliation('b34f24f3-51a4-4a00-8d4b-1a212a47c791', '95d7be81-0cfb-4b52-9c92-33a45747fcef');

SELECT upsert_user('b3519236-aaf5-4eef-84b7-724dc8289fb2', '7f4b6c63-8fbd-4927-8202-c6731c00a0fc', 'Viggos Vindmøller');
SELECT upsert_affiliation('b3519236-aaf5-4eef-84b7-724dc8289fb2', '7f4b6c63-8fbd-4927-8202-c6731c00a0fc');

SELECT upsert_user('df7bfcf7-c2c8-442b-83cc-b46e6e8c8e72', 'b0918bf8-626d-4b35-a0ad-3f5beb6b6fef', 'Fabrikant');
SELECT upsert_affiliation('df7bfcf7-c2c8-442b-83cc-b46e6e8c8e72', 'b0918bf8-626d-4b35-a0ad-3f5beb6b6fef');

SELECT upsert_user('ec738dea-6903-490d-b012-9a2fe98406dc', '21e150bd-8a0b-4ba6-905b-7223db248af5', 'Bolighaj');
SELECT upsert_affiliation('ec738dea-6903-490d-b012-9a2fe98406dc', '21e150bd-8a0b-4ba6-905b-7223db248af5');

COMMIT;
