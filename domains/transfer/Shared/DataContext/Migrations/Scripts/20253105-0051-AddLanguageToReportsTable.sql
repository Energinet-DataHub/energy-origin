DO
$$
BEGIN
  IF NOT EXISTS (
    SELECT 1
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name   = 'Reports'
       AND column_name  = 'Language'
  ) THEN
ALTER TABLE public."Reports"
    ADD COLUMN "Language" text DEFAULT 'English';

UPDATE public."Reports"
SET "Language" = 'English'
WHERE "Language" IS NULL;

ALTER TABLE public."Reports"
    ALTER COLUMN "Language" SET NOT NULL;

ALTER TABLE public."Reports"
    ALTER COLUMN "Language" DROP DEFAULT;
END IF;
END
$$;
