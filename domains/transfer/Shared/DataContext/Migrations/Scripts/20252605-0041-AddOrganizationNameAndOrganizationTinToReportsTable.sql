DO
$$
BEGIN
  --------------------------------------------------------------------------------
  -- 1) OrganizationName
  --------------------------------------------------------------------------------
  IF NOT EXISTS (
    SELECT 1
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name   = 'Reports'
       AND column_name  = 'OrganizationName'
  ) THEN
    -- a) add with default so new inserts get ''
ALTER TABLE public."Reports"
    ADD COLUMN "OrganizationName" text DEFAULT '';

-- b) backfill old rows
UPDATE public."Reports"
SET "OrganizationName" = ''
WHERE "OrganizationName" IS NULL;

-- c) enforce non-null
ALTER TABLE public."Reports"
    ALTER COLUMN "OrganizationName" SET NOT NULL;

-- d) drop the default if you don’t want it permanently
ALTER TABLE public."Reports"
    ALTER COLUMN "OrganizationName" DROP DEFAULT;
END IF;

  --------------------------------------------------------------------------------
  -- 2) OrganizationTin
  --------------------------------------------------------------------------------
  IF NOT EXISTS (
    SELECT 1
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name   = 'Reports'
       AND column_name  = 'OrganizationTin'
  ) THEN
    -- a) add with default so new inserts get ''
ALTER TABLE public."Reports"
    ADD COLUMN "OrganizationTin" text DEFAULT '';

-- b) backfill old rows
UPDATE public."Reports"
SET "OrganizationTin" = ''
WHERE "OrganizationTin" IS NULL;

-- c) enforce non-null
ALTER TABLE public."Reports"
    ALTER COLUMN "OrganizationTin" SET NOT NULL;

-- d) drop the default if you don’t want it permanently
ALTER TABLE public."Reports"
    ALTER COLUMN "OrganizationTin" DROP DEFAULT;
END IF;
END
$$;
