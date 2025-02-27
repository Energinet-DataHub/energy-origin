DO
$$
    BEGIN
        IF NOT EXISTS
            (SELECT 1
             FROM information_schema.tables
             WHERE table_schema = 'public'
               AND table_name = 'ActivityLogs')
        THEN

            CREATE TABLE public."ActivityLogs"
            (
                "Id"                    uuid                     NOT NULL,
                "Timestamp"             timestamp with time zone NOT NULL,
                "ActorId"               uuid                     NOT NULL,
                "ActorType"             integer                  NOT NULL,
                "ActorName"             text                     NOT NULL,
                "OrganizationTin"       text                     NOT NULL,
                "OrganizationName"      text                     NOT NULL,
                "EntityType"            integer                  NOT NULL,
                "ActionType"            integer                  NOT NULL,
                "EntityId"              text                     NOT NULL,
                "OtherOrganizationName" text DEFAULT ''::text NOT NULL,
                "OtherOrganizationTin"  text DEFAULT ''::text NOT NULL
            );

            ALTER TABLE ONLY public."ActivityLogs"
                ADD CONSTRAINT "PK_ActivityLogs" PRIMARY KEY ("Id");

            CREATE INDEX "IX_ActivityLogs_OrganizationTin" ON public."ActivityLogs" USING btree ("OrganizationTin");

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
               AND table_name = 'Contracts')
        THEN

            CREATE TABLE public."Contracts"
            (
                "Id"                  uuid                     NOT NULL,
                "ContractNumber"      integer                  NOT NULL,
                "GSRN"                text                     NOT NULL,
                "GridArea"            text                     NOT NULL,
                "MeteringPointType"   integer                  NOT NULL,
                "MeteringPointOwner"  text                     NOT NULL,
                "StartDate"           timestamp with time zone NOT NULL,
                "EndDate"             timestamp with time zone,
                "Created"             timestamp with time zone NOT NULL,
                "Technology_FuelCode" text,
                "Technology_TechCode" text,
                "RecipientId"         uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
            );

            ALTER TABLE ONLY public."Contracts"
                ADD CONSTRAINT "PK_Contracts" PRIMARY KEY ("Id");

            CREATE UNIQUE INDEX "IX_Contracts_GSRN_ContractNumber" ON public."Contracts" USING btree ("GSRN", "ContractNumber");

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
               AND table_name = 'MeteringPointTimeSeriesSlidingWindows')
        THEN

            CREATE TABLE public."MeteringPointTimeSeriesSlidingWindows"
            (
                "GSRN"                 text   NOT NULL,
                "SynchronizationPoint" bigint NOT NULL,
                "MissingMeasurements"  jsonb  NOT NULL
            );

            ALTER TABLE ONLY public."MeteringPointTimeSeriesSlidingWindows"
                ADD CONSTRAINT "PK_MeteringPointTimeSeriesSlidingWindows" PRIMARY KEY ("GSRN");

        END IF;
    END;
$$;

