DO
$$
BEGIN
  IF NOT EXISTS (
    SELECT 1
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name   = 'Clients'
       AND column_name  = 'IsTrial'
  ) THEN
    ALTER TABLE public."Clients"
        ADD COLUMN "IsTrial" boolean;

    UPDATE public."Clients" SET "IsTrial" = false WHERE "IsTrial" IS NULL;

    ALTER TABLE public."Clients"
        ALTER COLUMN "IsTrial" SET NOT NULL;

    ALTER TABLE public."Clients"
        ALTER COLUMN "IsTrial" DROP DEFAULT;
  END IF;
END
$$;

