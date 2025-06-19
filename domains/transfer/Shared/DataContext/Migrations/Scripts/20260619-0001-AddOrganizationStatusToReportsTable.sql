DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Reports'
          AND column_name = 'IsNormal'
    ) THEN
        ALTER TABLE public."Reports"
            ADD COLUMN "IsNormal" boolean DEFAULT FALSE;

        UPDATE public."Reports"
        SET "IsNormal" = FALSE
        WHERE "IsNormal" IS NULL;

        ALTER TABLE public."Reports"
            ALTER COLUMN "IsNormal" SET NOT NULL;

        ALTER TABLE public."Reports"
            ALTER COLUMN "IsNormal" DROP DEFAULT;
    END IF;
END
$$;
