BEGIN;

DO $$
DECLARE
relations_empty boolean;
BEGIN
SELECT COUNT(*) = 0 FROM public."Relations" INTO relations_empty;

IF relations_empty THEN
        INSERT INTO public."Relations" ("SubjectId", "Status", "Actor", "Tin") VALUES
            ('64cf9c0f-c634-41c0-9cba-4394024b915f', 1, 'dafab19f-e960-45a9-bc29-38683b736326', '11223344'),
            ('b0918bf8-626d-4b35-a0ad-3f5beb6b6fef', 1, '1c02e066-596e-4a30-abf3-4b28a6de8897', '55555555'),
            ('95d7be81-0cfb-4b52-9c92-33a45747fcef', 1, 'a6dc19df-7273-4e73-85e1-e9dbe7a527bd', '39293595'),
            ('7f4b6c63-8fbd-4927-8202-c6731c00a0fc', 1, '081dbd5f-adf9-4b98-bde6-5d35be120579', '77777777'),
            ('68237a9a-ff1c-48e1-83c5-d9857ef26587', 1, '6be9ecad-9d2f-40b2-ac75-df659e7bef8a', '28980671'),
            ('21e150bd-8a0b-4ba6-905b-7223db248af5', 1, '94bf1ca8-10eb-4e9f-a13d-e5675cf488f0', '66666666'),
            ('cb1e0981-1ea7-404e-b189-260c58a46c68', 1, '223cc2a6-5571-4f79-a7e9-4e0ae93440c1', '12345678');

        RAISE NOTICE 'Relations table seeded successfully.';
ELSE
        RAISE NOTICE 'Relations table is not empty. Skipping seed data insertion.';
END IF;
END $$;

COMMIT;
