DO
$$
    BEGIN
        IF NOT EXISTS
            (SELECT 1
             FROM information_schema.tables
             WHERE table_schema = 'public'
               AND table_name = 'Relations')
        THEN

            CREATE TABLE public."Relations" (
                                                "SubjectId" uuid NOT NULL,
                                                "Status" integer NOT NULL,
                                                "Actor" uuid NOT NULL,
                                                "Tin" text
            );

            ALTER TABLE ONLY public."Relations"
                ADD CONSTRAINT "PK_Relations" PRIMARY KEY ("SubjectId");

        END IF;
    END;
$$;
