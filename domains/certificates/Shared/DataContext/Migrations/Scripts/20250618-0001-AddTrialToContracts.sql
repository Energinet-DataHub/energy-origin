DO
$$
BEGIN

    IF NOT EXISTS (
        SELECT 1
        FROM   information_schema.columns
        WHERE  table_schema = 'public'
          AND  table_name   = 'Contracts'
          AND  column_name  = 'Trial'
    ) THEN

ALTER TABLE public."Contracts"
    ADD COLUMN "Trial" boolean;

UPDATE public."Contracts" SET "Trial" = FALSE WHERE "Trial" IS NULL;

END IF;
END;
$$;
