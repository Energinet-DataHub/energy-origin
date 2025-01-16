DO
$$
    BEGIN
        IF NOT EXISTS
            (SELECT 1
             FROM information_schema.tables
             WHERE table_schema = 'public'
               AND table_name = 'Organizations')
        THEN

            CREATE TABLE public."Organizations"
            (
                "Id"                                 uuid                  NOT NULL,
                "Tin"                                text,
                "Name"                               text                  NOT NULL,
                "TermsAcceptanceDate"                timestamp with time zone,
                "TermsAccepted"                      boolean DEFAULT false NOT NULL,
                "TermsVersion"                       integer,
                "ServiceProviderTermsAcceptanceDate" timestamp with time zone,
                "ServiceProviderTermsAccepted"       boolean DEFAULT false NOT NULL
            );

            ALTER TABLE ONLY public."Organizations"
                ADD CONSTRAINT "PK_Organizations" PRIMARY KEY ("Id");

            CREATE UNIQUE INDEX "IX_Organizations_Tin" ON public."Organizations" USING btree ("Tin");

        END IF;
    END;
$$;


DO
$$
    BEGIN
        IF NOT EXISTS
            (SELECT 1
             FROM information_schema.tables
             WHERE table_schema = 'public'
               AND table_name = 'Users')
        THEN

            CREATE TABLE public."Users"
            (
                "Id"        uuid NOT NULL,
                "IdpUserId" uuid NOT NULL,
                "Name"      text NOT NULL
            );

            ALTER TABLE ONLY public."Users"
                ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");

            CREATE UNIQUE INDEX "IX_Users_IdpUserId" ON public."Users" USING btree ("IdpUserId");

        END IF;
    END;
$$;


DO
$$
    BEGIN
        IF NOT EXISTS
            (SELECT 1
             FROM information_schema.tables
             WHERE table_schema = 'public'
               AND table_name = 'Affiliations')
        THEN

            CREATE TABLE public."Affiliations"
            (
                "UserId"         uuid NOT NULL,
                "OrganizationId" uuid NOT NULL
            );

            ALTER TABLE ONLY public."Affiliations"
                ADD CONSTRAINT "FK_Affiliations_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations" ("Id") ON DELETE CASCADE;

            ALTER TABLE ONLY public."Affiliations"
                ADD CONSTRAINT "PK_Affiliations" PRIMARY KEY ("UserId", "OrganizationId");

            CREATE INDEX "IX_Affiliations_OrganizationId" ON public."Affiliations" USING btree ("OrganizationId");

            ALTER TABLE ONLY public."Affiliations"
                ADD CONSTRAINT "FK_Affiliations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users" ("Id") ON DELETE CASCADE;

        END IF;
    END;
$$;


DO
$$
    BEGIN
        IF NOT EXISTS
            (SELECT 1
             FROM information_schema.tables
             WHERE table_schema = 'public'
               AND table_name = 'Clients')
        THEN

            CREATE TABLE public."Clients"
            (
                "Id"             uuid    NOT NULL,
                "IdpClientId"    uuid    NOT NULL,
                "Name"           text    NOT NULL,
                "ClientType"     integer NOT NULL,
                "RedirectUrl"    text    NOT NULL,
                "OrganizationId" uuid
            );

            ALTER TABLE ONLY public."Clients"
                ADD CONSTRAINT "PK_Clients" PRIMARY KEY ("Id");

            CREATE UNIQUE INDEX "IX_Clients_IdpClientId" ON public."Clients" USING btree ("IdpClientId");

            CREATE INDEX "IX_Clients_OrganizationId" ON public."Clients" USING btree ("OrganizationId");

            ALTER TABLE ONLY public."Clients"
                ADD CONSTRAINT "FK_Clients_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations" ("Id");

        END IF;
    END;
$$;

DO
$$
    BEGIN
        IF NOT EXISTS
            (SELECT 1
             FROM information_schema.tables
             WHERE table_schema = 'public'
               AND table_name = 'OrganizationConsents')
        THEN

            CREATE TABLE public."OrganizationConsents"
            (
                "Id"                            uuid                     NOT NULL,
                "ConsentGiverOrganizationId"    uuid                     NOT NULL,
                "ConsentReceiverOrganizationId" uuid                     NOT NULL,
                "ConsentDate"                   timestamp with time zone NOT NULL
            );

            ALTER TABLE ONLY public."OrganizationConsents"
                ADD CONSTRAINT "PK_OrganizationConsents" PRIMARY KEY ("Id");

            CREATE INDEX "IX_OrganizationConsents_ConsentGiverOrganizationId" ON public."OrganizationConsents" USING btree ("ConsentGiverOrganizationId");

            CREATE UNIQUE INDEX "IX_OrganizationConsents_ConsentReceiverOrganizationId_ConsentG~" ON public."OrganizationConsents" USING btree ("ConsentReceiverOrganizationId", "ConsentGiverOrganizationId");

            ALTER TABLE ONLY public."OrganizationConsents"
                ADD CONSTRAINT "FK_OrganizationConsents_Organizations_ConsentGiverOrganization~" FOREIGN KEY ("ConsentGiverOrganizationId") REFERENCES public."Organizations" ("Id") ON DELETE CASCADE;

            ALTER TABLE ONLY public."OrganizationConsents"
                ADD CONSTRAINT "FK_OrganizationConsents_Organizations_ConsentReceiverOrganizat~" FOREIGN KEY ("ConsentReceiverOrganizationId") REFERENCES public."Organizations" ("Id") ON DELETE CASCADE;

        END IF;
    END;
$$;


DO
$$
    BEGIN
        IF NOT EXISTS
            (SELECT 1
             FROM information_schema.tables
             WHERE table_schema = 'public'
               AND table_name = 'Terms')
        THEN

            CREATE TABLE public."Terms"
            (
                "Id"      uuid    NOT NULL,
                "Version" integer NOT NULL
            );

            ALTER TABLE ONLY public."Terms"
                ADD CONSTRAINT "PK_Terms" PRIMARY KEY ("Id");

            CREATE UNIQUE INDEX "IX_Terms_Version" ON public."Terms" USING btree ("Version");

        END IF;
    END;
$$;
