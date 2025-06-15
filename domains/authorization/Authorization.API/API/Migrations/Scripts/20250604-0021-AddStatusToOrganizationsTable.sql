DO
$$
BEGIN
  IF NOT EXISTS (
    SELECT 1
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name   = 'Organizations'
       AND column_name  = 'Status'
  ) THEN
ALTER TABLE public."Organizations"
    ADD COLUMN "Status" text DEFAULT 'Normal';

UPDATE public."Organizations"
SET "Status" = 'Normal'
WHERE "Status" IS NULL;

ALTER TABLE public."Organizations"
    ALTER COLUMN "Status" SET NOT NULL;

ALTER TABLE public."Organizations"
    ALTER COLUMN "Status" DROP DEFAULT;
END IF;
END
$$;
