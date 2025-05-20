DO
$$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.tables
     WHERE table_schema = 'public' AND table_name = 'Reports'
  ) THEN
CREATE TABLE public."Reports" (
                                  "Id"              uuid NOT NULL,
                                  "CreatedAt"       timestamp with time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') NOT NULL,
                                  "StartDate"       timestamp with time zone NOT NULL,
                                  "EndDate"         timestamp with time zone NOT NULL,
                                  "Status"          text NOT NULL,
                                  "Content"         bytea,
                                  "OrganizationId"  uuid NOT NULL,
                                  CONSTRAINT "PK_Reports" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_Reports_OrganizationId"
    ON public."Reports" ("OrganizationId");
END IF;
END;
$$;
