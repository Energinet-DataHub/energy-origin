# TODO all diagrams should be revised to resemble Energy Origin as-is

apiGateway = container "API Gateway" {
    description "Routes requests to services and forwards authentication requests"
    technology "Traefik"
}

dataHubFacadeDomain = group "DataHubFacade Domain" {
    dataHubFacadeApi = container "DataHubFacade" {
        description "Facade for DataHub 2.0 and 3.0"
        technology ".NET"

        this -> dh2 "Forwards requests to"
        this -> dh3 "Forwards requests to"
    }
}

authDomain = group "Auth Domain" {
    authApi = container "Auth Web Api" {
        description "API For authentication and authorization"

        this -> mitId "Executes UIDC callbacks"
        apiGateway -> this "Forwards requests to"
        this -> dataHubFacadeApi "Creates relations for metering points in"
    }

    authDb = container "Database" {
        tags "Data Storage"
        description "Stores user details and settings"
        technology "PostgreSQL"

        authApi -> this "Reads projected user data from"
    }
}

certificatesDomain = group "Certificate Domain" {
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
        description "Coordinates issurance between registry and wallet"

        this -> poRegistry "Sends issued events to"
        this -> poWallet "Sends slices to"
        this -> certRabbitMq "Produces and consumes messages using"
    }
    certApi = container "Certificate API" {
        description "Contains background workers for fetching measurements and issuing a certificate and provides an API for queries related to certificates and contracts"
        technology ".NET Web Api"

        contractService = component "ContractService" "Handles contracts for generation of certificates" "Service" {
            this -> dataHubFacadeApi "Get metering point info from"
            this -> certStorage "Stores contracts in"
            this -> poWallet "Creates Wallet Deposit Endpoints"
        }
        dataSyncSyncer = component "DataSyncSyncer" "Fetches measurements every hour and publishes to the message broker. ONLY NEED UNTIL INTEGRATION EVENT BUS HAS EVENTS FOR MEASUREMENTS." "Hosted background service" {
            tags "MockingComponent"

            this -> certRabbitMq "Publishes measurement events to"
            this -> contractService "Reads list of metering points to sync from"
            this -> dataHubFacadeApi "Pulls measurements from"
        }
        granularCertificateIssuer = component "GranularCertificateIssuer" "Based on a measurement point and metadata, creates a certificate event" "Message consumer" {
            this -> contractService "Checks for a valid contract in"
            this -> certRabbitMq "Subscribes to measurement event from"
            this -> certStorage "Saves information about issued certificate in"
        }
        certQueryAPI = component "Query API" "API for issuing contracts and proxies requests to Wallet" "ASP.NET Core WebAPI" {
            this -> contractService "Reads contracts from"
            this -> poWallet "Reads certificates from"
        }

        apiGateway -> this "Forwards requests to"
    }
}

measurementsDomain = group "Measurements Domain" {
    measurementApi = container "Measurements Web Api" {
        description "API for aggregated measurements split into production and consumption"

        apiGateway -> this "Forwards requests to"

        measurementService = component "MeasurementService" "bla."
        meteringPointService = component "MeteringPointService" "bla."

        this -> dataHubFacadeApi "Get measurements from"
    }
}

transferDomain = group "Transfer Domain" {
    tApi = container "Transfer API" "" ".NET Web Api" {
        connectionsApi = component "Connections Api" "Allows users to see connections of their company." ".NET Web Api"
        transferAgreementsApi = component "Transfer Agreements Api" "Allows users to create transfer agreements with other companies" ".NET Web Api" {
            this -> poWallet "Creates wallet deposit endpoint"
        }
        deleteConnectionInvitationsWorker = component "Delete Connection Invitations Worker" "Deletes expired connection invitations" ".NET BackgroundService"
        transferAgreementAutomation = component "Transfer Agreements Automation" "Transfers certificates within a given transfer agreement" ".NET BackgroundService" {
            this -> poWallet "Transfers certificates"
        }
        cvrProxy = component "CVR proxy" "Gets CVR data" ".NET Web Api"
    }
    tDb = container "Transfer Storage" {
        tags "Data Storage"
        description ""
        technology "Postgres SQL"

        tApi -> this "Saves and reads transfer agreement and connections data"
    }
    apiGateway -> connectionsApi "Forwards requests to"
    apiGateway -> transferAgreementsApi "Forwards requests to"
    apiGateway -> cvrProxy "Forwards requests to"
    connectionsApi -> tDb "Stores connections"
    transferAgreementsApi -> tDb "Stores transfer agreements"
    transferAgreementAutomation -> tDb "Reads transfer agreements"
    deleteConnectionInvitationsWorker -> tDb "Deletes connection invitations"
    cvrProxy -> cvr "Forwards requests to"
}

apiGateway -> dataSyncApi "Forwards requests for metering point info to"
