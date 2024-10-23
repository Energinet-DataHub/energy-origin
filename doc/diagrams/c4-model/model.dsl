apiGateway = container "API Gateway" {
    description "Routes requests to services and forwards authentication requests"
    technology "Traefik"
}

dataHubFacadeSubsystem = group "DataHubFacade Subsystem" {
    dataHubFacadeApi = container "DataHubFacade" {
        description "Facade for DataHub 2.0 and 3.0. Simplifies interaction so clients do not have to handle DataHub certificate, SOAP parsing, pagination and Danish time zone convertion"
        technology ".NET"

        this -> dh2 "Forwards requests to"
        this -> dh3 "Forwards requests to"
    }
}

authSubsystem = group "Authorization Subsystem" {
    authApi = container "Authorization Web Api" {
        description "API For authentication and authorization"

        this -> mitId "Gets user info"
        apiGateway -> this "Forwards requests to"
        azureAdB2c -> this "Gets user info, consents and claims"
    }

    authDb = container "Database" {
        tags "Data Storage"
        description "Stores user details and settings"
        technology "PostgreSQL"

        authApi -> this "Reads projected user data from"
    }
}

measurementsSubsystem = group "Measurements Subsystem" {
    measurementApi = container "Measurements Web Api" {
        description "API for aggregated measurements split into production and consumption"

        apiGateway -> this "Forwards requests to"

        measurementService = component "MeasurementService" "Handles representation of measurements within Energy Track and Trace DK" {
            this -> dataHubFacadeApi "Get measurements from"
        }
        meteringPointService = component "MeteringPointService" "Handles representation of metering points within Energy Track and Trace DK" {
            apiGateway -> this "Forwards requests to"
            this -> dataHubFacadeApi "Get metering point info from"
            authApi -> this "Publishes accepted terms via RabbitMq to"
        }
    }
}

certificatesSubsystem = group "Certificate Subsystem" {
    certStorage = container "Certificate Storage" {
        tags "Data Storage"
        description "Storage for contracts and information from the issuance of a certificate"
        technology "Postgres"
    }
    certApi = container "Certificate API" {
        description "Contains background workers for fetching measurements and provides an API for queries related to contracts"
        technology ".NET Web Api"

        contractService = component "ContractService" "Handles contracts for generation of certificates" "Service" {
            this -> measurementApi "Get metering point info from"
            this -> certStorage "Stores contracts in"
            this -> poWallet "Creates Wallet Endpoints"
        }
        measurementsSyncer = component "Measurements Syncer" "Fetches measurements every hour and publishes to the message broker. ONLY NEED UNTIL INTEGRATION EVENT BUS HAS EVENTS FOR MEASUREMENTS." "Hosted background service" {
            tags "MockingComponent"

            this -> poStamp "Sends measurements to stamp, which becomes certificates"
            this -> contractService "Reads list of metering points to sync from"
            this -> measurementApi "Pulls measurements from"
        }
        certQueryAPI = component "Query API" "API for issuing contracts and proxies requests to Wallet" "ASP.NET Core WebAPI" {
            this -> contractService "Reads contracts from"
            this -> poWallet "Reads certificates from"
        }

        apiGateway -> this "Forwards requests to"
    }
}

transferSubsystem = group "Transfer Subsystem" {
    transferApi = container "Transfer API" "" ".NET Web Api" {
        connectionsApi = component "Connections Api" "Allows users to see connections of their company." ".NET Web Api"
        transferAgreementsApi = component "Transfer Agreements Api" "Allows users to create transfer agreements with other companies" ".NET Web Api" {
            this -> poWallet "Creates wallet endpoints"
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
