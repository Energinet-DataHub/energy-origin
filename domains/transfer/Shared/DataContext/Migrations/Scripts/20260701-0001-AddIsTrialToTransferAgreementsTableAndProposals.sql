DO
$$
BEGIN
IF
NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'TransferAgreement'
          AND column_name = 'IsTrial'
    ) THEN
ALTER TABLE public."TransferAgreements"
    ADD COLUMN "IsTrial" boolean DEFAULT FALSE;
END IF;
IF
NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'TransferAgreementProposals'
          AND column_name = 'IsTrial'
    ) THEN
ALTER TABLE public."TransferAgreementProposals"
    ADD COLUMN "IsTrial" boolean DEFAULT FALSE;
END IF;

END
$$;
