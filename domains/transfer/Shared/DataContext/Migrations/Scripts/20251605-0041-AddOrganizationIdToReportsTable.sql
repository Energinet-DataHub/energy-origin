DO
$$
BEGIN
  IF NOT EXISTS (
    SELECT 1
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name   = 'Reports'
       AND column_name  = 'OrganizationId'
  ) THEN
ALTER TABLE public."Reports"
    ADD COLUMN "OrganizationId" uuid;
END IF;

  IF EXISTS (
    SELECT 1
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name   = 'Reports'
       AND column_name  = 'OrganizationId'
       AND is_nullable  = 'YES'
  ) THEN
ALTER TABLE public."Reports"
    ALTER COLUMN "OrganizationId" SET NOT NULL;
END IF;

  IF NOT EXISTS (
    SELECT 1
      FROM pg_class c
      JOIN pg_namespace n ON n.oid = c.relnamespace
     WHERE c.relkind = 'i'
       AND c.relname = 'IX_Reports_OrganizationId'
       AND n.nspname = 'public'
  ) THEN
CREATE INDEX "IX_Reports_OrganizationId"
    ON public."Reports" ("OrganizationId");
END IF;
END;
$$;
