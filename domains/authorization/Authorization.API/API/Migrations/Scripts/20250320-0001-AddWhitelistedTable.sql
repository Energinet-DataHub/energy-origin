DO
$$
    BEGIN
        IF NOT EXISTS
            (SELECT 1
             FROM information_schema.tables
             WHERE table_schema = 'public'
               AND table_name = 'Whitelisted')
        THEN

            CREATE TABLE public."Whitelisted"
            (
                "Id"   uuid   NOT NULL,
                "Tin"  text   NOT NULL
            );

            ALTER TABLE ONLY public."Whitelisted"
                ADD CONSTRAINT "PK_Whitelisted" PRIMARY KEY ("Id");

            CREATE UNIQUE INDEX "IX_Whitelisted_Tin" ON public."Whitelisted" USING btree ("Tin");

        END IF;
    END;
$$;
