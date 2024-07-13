DO $$
DECLARE
current_date_time timestamp with time zone;
    ninety_days_ago timestamp with time zone;
BEGIN
    current_date_time := CURRENT_TIMESTAMP;

    ninety_days_ago := current_date_time - INTERVAL '90 days';

INSERT INTO public."Contracts" ("Id", "ContractNumber", "GSRN", "GridArea", "MeteringPointType", "MeteringPointOwner", "StartDate", "EndDate", "Created", "WalletUrl", "WalletPublicKey", "Technology_FuelCode", "Technology_TechCode")
VALUES (
           '3642dd9e-1ddd-4b97-bd93-9e0ab02ce703',
           0,
           '571313130083535430',
           'DK1',
           0,
           '64cf9c0f-c634-41c0-9cba-4394024b915f',
           ninety_days_ago + ('2024-06-29 16:14:26+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone),
           NULL,
           ninety_days_ago + ('2024-06-29 16:14:27.679953+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone),
           'WALLET_URL_PLACEHOLDER',
           WALLET_PUBLIC_KEY_PLACEHOLDER,
           'F01050100',
           'T020000'
       ) ON CONFLICT DO NOTHING;
END $$;
