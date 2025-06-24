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
    ADD COLUMN "IsTrial" boolean DEFAULT false NOT NULL;

END IF;
END
$$;

