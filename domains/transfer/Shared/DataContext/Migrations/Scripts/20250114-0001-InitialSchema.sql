DO
$$
    BEGIN
        IF NOT EXISTS
            (SELECT 1
             FROM information_schema.tables
             WHERE table_schema = 'public'
               AND table_name = 'ClaimAutomationArguments')
        THEN

            CREATE TABLE public."ClaimAutomationArguments" (
               "SubjectId" uuid NOT NULL,
               "CreatedAt" timestamp with time zone NOT NULL
            );

            ALTER TABLE ONLY public."ClaimAutomationArguments"
                ADD CONSTRAINT "PK_ClaimAutomationArguments" PRIMARY KEY ("SubjectId");

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
               AND table_name = 'TransferAgreementProposals')
        THEN

            CREATE TABLE public."TransferAgreementProposals" (
                "Id" uuid NOT NULL,
                "SenderCompanyId" uuid NOT NULL,
                "SenderCompanyTin" text NOT NULL,
                "SenderCompanyName" text NOT NULL,
                "CreatedAt" timestamp with time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'UTC'::text) NOT NULL,
                "StartDate" timestamp with time zone NOT NULL,
                "EndDate" timestamp with time zone,
                "ReceiverCompanyTin" text,
                "Type" integer DEFAULT 0 NOT NULL
            );

            ALTER TABLE ONLY public."TransferAgreementProposals"
                ADD CONSTRAINT "PK_TransferAgreementProposals" PRIMARY KEY ("Id");

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
               AND table_name = 'TransferAgreements')
        THEN

            CREATE TABLE public."TransferAgreements" (
                "Id" uuid NOT NULL,
                "StartDate" timestamp with time zone NOT NULL,
                "EndDate" timestamp with time zone,
                "ReceiverTin" text NOT NULL,
                "SenderId" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
                "SenderName" text DEFAULT ''::text NOT NULL,
                "SenderTin" text DEFAULT ''::text NOT NULL,
                "ReceiverReference" uuid NOT NULL,
                "TransferAgreementNumber" integer DEFAULT 0 NOT NULL,
                "ReceiverName" text DEFAULT ''::text NOT NULL,
                "Type" integer DEFAULT 0 NOT NULL,
                "ReceiverId" uuid
            );

            ALTER TABLE ONLY public."TransferAgreements"
                ADD CONSTRAINT "PK_TransferAgreements" PRIMARY KEY ("Id");

            CREATE UNIQUE INDEX "IX_TransferAgreements_SenderId_TransferAgreementNumber" ON public."TransferAgreements" USING btree ("SenderId", "TransferAgreementNumber");

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
