DO
$$
BEGIN
  IF NOT EXISTS (
    SELECT 1
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name   = 'Reports'
       AND column_name  = 'OrganizationName'
  ) THEN
ALTER TABLE public."Reports"
    ADD COLUMN "OrganizationName" text DEFAULT '';

UPDATE public."Reports"
SET "OrganizationName" = ''
WHERE "OrganizationName" IS NULL;

ALTER TABLE public."Reports"
    ALTER COLUMN "OrganizationName" SET NOT NULL;

ALTER TABLE public."Reports"
    ALTER COLUMN "OrganizationName" DROP DEFAULT;
END IF;

  IF NOT EXISTS (
    SELECT 1
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name   = 'Reports'
       AND column_name  = 'OrganizationTin'
  ) THEN
ALTER TABLE public."Reports"
    ADD COLUMN "OrganizationTin" text DEFAULT '';

UPDATE public."Reports"
SET "OrganizationTin" = ''
WHERE "OrganizationTin" IS NULL;

ALTER TABLE public."Reports"
    ALTER COLUMN "OrganizationTin" SET NOT NULL;

ALTER TABLE public."Reports"
    ALTER COLUMN "OrganizationTin" DROP DEFAULT;
END IF;
END
$$;
