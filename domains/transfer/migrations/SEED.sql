DO $$
DECLARE
activity_logs_empty boolean;
    claim_automation_arguments_empty boolean;
    transfer_agreements_empty boolean;
    current_date_time timestamp with time zone;
    ninety_days_ago timestamp with time zone;
BEGIN

    current_date_time := CURRENT_TIMESTAMP;

    ninety_days_ago := current_date_time - INTERVAL '90 days';

SELECT COUNT(*) = 0 FROM "ActivityLogs" INTO activity_logs_empty;
IF activity_logs_empty THEN
        INSERT INTO public."ActivityLogs" ("Id", "Timestamp", "ActorId", "ActorType", "ActorName", "OrganizationTin", "OrganizationName", "EntityType", "ActionType", "EntityId", "OtherOrganizationName", "OtherOrganizationName") VALUES
            ('13497eec-131f-4bea-9eba-c44958cc36ad', ninety_days_ago + ('2024-06-29 16:15:47.876339+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone), '64cf9c0f-c634-41c0-9cba-4394024b915f', 0, 'Peter Producent', '11223344', 'Producent A/S', 1, 0, '1512a3bf-b241-48b2-8f4c-868aa017062c', '', '39293595'),
            ('98ae93ca-0b23-4e8a-ac9d-fc372aa17c9d', ninety_days_ago + ('2024-06-29 16:15:58.657819+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone), '00000000-0000-0000-0000-000000000000', 0, '', '11223344', 'Producent A/S', 0, 1, 'f226b326-2ab6-45a1-96fb-78668318f357', 'Company Inc.', '39293595'),
            ('9eeaefe0-2e4d-4fbe-a739-a4e96b708e53', ninety_days_ago + ('2024-06-29 16:15:58.65745+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone), '95d7be81-0cfb-4b52-9c92-33a45747fcef', 0, 'Charlotte C.S. Rasmussen', '39293595', 'Company Inc.', 0, 1, 'f226b326-2ab6-45a1-96fb-78668318f357', 'Producent A/S', '11223344'),
            ('c35fef95-c697-4ed3-95f4-5b6200719e96', ninety_days_ago + ('2024-06-29 16:26:10.589956+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone), '7f4b6c63-8fbd-4927-8202-c6731c00a0fc', 0, 'Viggos Vindmøller', '77777777', 'Viggos Vindmøller', 1, 0, 'e09b65f9-dc13-482b-baff-02604f7e2100', '', '66666666'),
            ('0af17f20-6061-4a7c-8288-97ebf7345c28', ninety_days_ago + ('2024-06-29 16:26:31.691116+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone), '21e150bd-8a0b-4ba6-905b-7223db248af5', 0, 'Bolighaj', '66666666', 'Bolighaj', 0, 1, 'fb3594bf-0d3f-445f-923a-ea59a5fc42d2', 'Viggos Vindmøller', '77777777'),
            ('7ac2f267-176b-4092-9c3b-f1797fccde05', ninety_days_ago + ('2024-06-29 16:26:31.691214+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone), '00000000-0000-0000-0000-000000000000', 0, '', '77777777', 'Viggos Vindmøller', 0, 1, 'fb3594bf-0d3f-445f-923a-ea59a5fc42d2', 'Bolighaj', '66666666')
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'ActivityLogs table seeded successfully.';
ELSE
        RAISE NOTICE 'ActivityLogs table is not empty. Skipping seed data insertion.';
END IF;

SELECT COUNT(*) = 0 FROM "ClaimAutomationArguments" INTO claim_automation_arguments_empty;
IF claim_automation_arguments_empty THEN
        INSERT INTO public."ClaimAutomationArguments" ("SubjectId", "CreatedAt") VALUES
            ('95d7be81-0cfb-4b52-9c92-33a45747fcef', ninety_days_ago + ('2024-06-29 16:15:14.91644+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone)),
            ('b0918bf8-626d-4b35-a0ad-3f5beb6b6fef', ninety_days_ago + ('2024-06-29 16:22:24.62554+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone)),
            ('21e150bd-8a0b-4ba6-905b-7223db248af5', ninety_days_ago + ('2024-06-29 16:24:59.114491+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone))
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'ClaimAutomationArguments table seeded successfully.';
ELSE
        RAISE NOTICE 'ClaimAutomationArguments table is not empty. Skipping seed data insertion.';
END IF;

SELECT COUNT(*) = 0 FROM "TransferAgreements" INTO transfer_agreements_empty;
IF transfer_agreements_empty THEN
    INSERT INTO public."TransferAgreements" ("Id", "StartDate", "EndDate", "ReceiverTin", "SenderId", "SenderName", "SenderTin", "ReceiverReference", "TransferAgreementNumber", "ReceiverName") VALUES
        ('f226b326-2ab6-45a1-96fb-78668318f357',
         date_trunc('hour', ninety_days_ago + ('2024-06-29 16:15:58.657819+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone)) + INTERVAL '1 hour',
         NULL, '39293595', '64cf9c0f-c634-41c0-9cba-4394024b915f', 'Producent A/S', '11223344', '9fe7331e-9b10-4479-99bb-0d6db3c69089', 0, 'Company Inc.'),
        ('fb3594bf-0d3f-445f-923a-ea59a5fc42d2',
         date_trunc('hour', ninety_days_ago + ('2024-06-29 16:26:31.691214+00'::timestamp with time zone - '2024-06-29'::timestamp with time zone)) + INTERVAL '1 hour',
         NULL, '66666666', '7f4b6c63-8fbd-4927-8202-c6731c00a0fc', 'Viggos Vindmøller', '77777777', 'a5a1ced5-022e-4012-b294-e55744aabb4b', 0, 'Bolighaj')
    ON CONFLICT DO NOTHING;
    RAISE NOTICE 'TransferAgreements table seeded successfully.';
ELSE
    RAISE NOTICE 'TransferAgreements table is not empty. Skipping seed data insertion.';
END IF;

END $$;
