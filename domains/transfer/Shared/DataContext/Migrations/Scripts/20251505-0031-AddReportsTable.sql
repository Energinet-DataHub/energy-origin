DO
$$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.tables
     WHERE table_schema = 'public' AND table_name = 'Reports'
  ) THEN
CREATE TABLE public."Reports" (
                                  "Id"         uuid NOT NULL,
                                  "CreatedAt"  timestamp with time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'UTC'::text) NOT NULL,
                                  "StartDate"  timestamp with time zone NOT NULL,
                                  "EndDate"    timestamp with time zone NOT NULL,
                                  "Status"     text NOT NULL,
                                  "Content"    bytea
);
ALTER TABLE public."Reports"
    ADD CONSTRAINT "PK_Reports" PRIMARY KEY ("Id");
END IF;
END;
$$;
