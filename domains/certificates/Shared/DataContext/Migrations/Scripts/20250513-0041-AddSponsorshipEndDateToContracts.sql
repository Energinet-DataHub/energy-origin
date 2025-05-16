DO
$$
BEGIN

    IF NOT EXISTS (
        SELECT 1
        FROM   information_schema.columns
        WHERE  table_schema = 'public'
          AND  table_name   = 'Contracts'
          AND  column_name  = 'SponsorshipEndDate'
    ) THEN

ALTER TABLE public."Contracts"
    ADD COLUMN "SponsorshipEndDate" timestamp with time zone NULL;

END IF;
END;
$$;
