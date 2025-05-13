DO
$$
BEGIN
    IF NOT EXISTS (
        SELECT 1
          FROM information_schema.tables
         WHERE table_schema = 'public'
           AND table_name   = 'Sponsorships'
    ) THEN

CREATE TABLE public."Sponsorships"
(
    "SponsorshipGSRN"           TEXT                     NOT NULL,
    "SponsorshipEndDate"        timestamp with time zone NOT NULL,

    CONSTRAINT "PK_Sponsorships" PRIMARY KEY ("SponsorshipGSRN")
);

END IF;
END;
$$;
