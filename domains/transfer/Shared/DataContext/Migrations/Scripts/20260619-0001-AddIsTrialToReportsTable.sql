DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Reports'
          AND column_name = 'IsTrial'
    ) THEN
        ALTER TABLE public."Reports"
            ADD COLUMN "IsTrial" boolean DEFAULT FALSE;

        UPDATE public."Reports"
        SET "IsTrial" = FALSE
        WHERE "IsTrial" IS NULL;

        ALTER TABLE public."Reports"
            ALTER COLUMN "IsTrial" SET NOT NULL;

        ALTER TABLE public."Reports"
            ALTER COLUMN "IsTrial" DROP DEFAULT;
    END IF;
END
$$;
