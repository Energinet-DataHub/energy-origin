apiGateway = container "API Gateway" {
    description "Routes requests to services and forwards authentication requests"
    technology "Traefik"
}

dataSyncSubsystem = group "Data Sync" {
    dataSyncApi = container "Data Sync" {
        description "Facade for DataHub 2.0"
        technology ".NET"

        this -> dh2 "Forwards requests to"
    }
}

authSubsystem = group "Auth Subsystem" {
    authApi = container "Auth Web Api" {
        description "API For authentication and authorization"

        this -> mitId "Executes UIDC callbacks"
        apiGateway -> this "Forwards requests to"
        this -> dataSyncApi "Creates relations for metering points in"
    }

    authDb = container "Database" {
        tags "Data Storage"
        description "Stores user details and settings"
        technology "PostgreSQL"

        authApi -> this "Reads projected user data from"
    }
}

certificatesSubsystem = group "Certificate Subsystem" {
    certRabbitMq = container "Certificate Message Broker" {
        description ""
        technology "RabbitMQ"
    }
    certStorage = container "Certificate Storage" {
        tags "Data Storage"
        description "Storage for contracts and information from the issuance of a certificate"
        technology "Postgres"
    }
    certRegistryConnector = container "Registry Connector" {
        description "Handles the issuence flow"

        this -> poRegistry "Sends issued events to"
        this -> poWallet "Sends slices to"
        this -> certRabbitMq "Produces and consumes messages"
    }
    certApi = container "Certificate API" {
        description "Contains background workers for fetching measurements and provides an API for queries related to contracts"
        technology ".NET Web Api"

        contractService = component "ContractService" "Handles contracts for generation of certificates" "Service" {
            this -> dataSyncApi "Get metering point info from"
            this -> certStorage "Stores contracts in"
            this -> poWallet "Creates Wallet Deposit Endpoints"
        }
        dataSyncSyncer = component "DataSyncSyncer" "Fetches measurements every hour and publishes to the message broker. ONLY NEED UNTIL INTEGRATION EVENT BUS HAS EVENTS FOR MEASUREMENTS." "Hosted background service" {
            tags "MockingComponent"

            this -> certRabbitMq "Publishes measurement events to"
            this -> contractService "Reads list of metering points to sync from"
            this -> dataSyncApi "Pulls measurements from"
        }
        certQueryAPI = component "Query API" "API for issuing contracts and proxies requests to Wallet" "ASP.NET Core WebAPI" {
            this -> contractService "Reads contracts from"
            this -> poWallet "Reads certificates from"
        }

        apiGateway -> this "Forwards requests to"
    }
}

measurementsSubsystem = group "Measurements Subsystem" {
    measurementApi = container "Measurements Web Api" {
        description "API for aggregated measurements split into production and consumption"

        apiGateway -> this "Forwards requests to"
        this -> dataSyncApi "Get measurements from"
    }
}

transferSubsystem = group "Transfer Subsystem" {
    transferApi = container "Transfer API" "" ".NET Web Api" {
        transferAgreementsApi = component "Transfer Agreements Api" "Allows users to create transfer agreements with other companies" ".NET Web Api" {
            this -> poWallet "Creates wallet deposit endpoint"
        }
        deleteTransferAgreementProposalsWorker = component "Delete Transfer Agreement Proposals Worker" "Deletes expired Transfer Agreement Proposals" ".NET BackgroundService"
        transferAgreementAutomation = component "Transfer Agreements Automation" "Transfers certificates within a given transfer agreement" ".NET BackgroundService" {
            this -> poWallet "Transfers certificates"
        }
        cvrProxy = component "CVR proxy" "Gets CVR data" ".NET Web Api"
        claimAutomation = component "Claim Automation" "Claims certificates" ".NET BackgroundService" {
            this -> poWallet "Claims certificates"
        }
        claimAutomationApi = component "Claim Automation Api" "Allows users to start, stop and see status of claim automation for the company" ".NET Web Api"
    }
    tDb = container "Transfer Storage" {
        tags "Data Storage"
        description ""
        technology "Postgres SQL"

        transferApi -> this "Saves and reads transfer agreement data"
    }
    apiGateway -> transferAgreementsApi "Forwards requests to"
    apiGateway -> cvrProxy "Forwards requests to"
    apiGateway -> claimAutomationApi "Forwards requests to"
    transferAgreementsApi -> tDb "Stores transfer agreements"
    transferAgreementAutomation -> tDb "Reads transfer agreements"
    deleteTransferAgreementProposalsWorker -> tDb "Deletes transfer agreement proposals"
    cvrProxy -> cvr "Forwards requests to"
    claimAutomation -> tDb "Reads owner ids"
    claimAutomationApi -> tDb "Creates, deletes and reads owner ids"
}

apiGateway -> dataSyncApi "Forwards requests for metering point info to"
