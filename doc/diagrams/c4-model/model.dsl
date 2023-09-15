# TODO all diagrams should be revised to resemble Energy Origin as-is

apiGateway = container "API Gateway" {
    description "Routes requests to services and forwards authentication requests"
    technology "Traefik"
}

dataSyncDomain = group "Data Sync" {
    dataSyncApi = container "Data Sync" {
        description "Facade for DataHub 2.0"
        technology ".NET"

        this -> dh2 "Forwards requests to"
    }
}

authDomain = group "Auth Domain" {
    authApi = container "Auth Web Api" {
        description "API For authentication and authorization"

        this -> mitId "Executes UIDC callbacks"
        apiGateway -> this "Forwards requests to"
        this -> dataSyncApi "Creates relations for metering points in"
    }

    authDb = container "Database" {
        description "Stores user details and settings"
        technology "PostgreSQL"

        authApi -> this "Reads projected user data from"
    }
}

certificatesDomain = group "Certificate Domain" {
    certApi = container "Certificate API" {
        description ""
        technology ".NET Web Api"

        apiGateway -> this "Forwards requests to"
        this -> dataSyncApi "Reads measurements and metering points from"
    }
    certRegistryConnector = container "Registry Connector" {
        description "Coordinates issurance between registry and wallet"

        this -> po "Sends issued events to registry and slices to wallet"
    }
    certRabbitMq = container "Certificate Message Broker" {
        description ""
        technology "RabbitMQ"

        certApi -> this "Produces and consumes messages using"
        certRegistryConnector -> this "Produces and consumes messages using"
    }
    certEventStore = container "Certificate Storage" {
        description "Storage for contracts and information from the issuance of a certificate"
        technology "Postgres"

        certApi -> this "Saves and reads issuing contracts using"
    }
}

emissionsDomain = group "Emissions Domain" {

}

measurementsDomain = group "Measurements Domain" {

}

transferDomain = group "Transfer Domain" {
    tApi = container "Transfer API" "" ".NET Web Api" {
        connectionsApi = component "Connections Api" "Allows users to see connections of their company." ".NET Web Api"
        transferAgreementsApi = component "Transfer Agreements Api" "Allows users to create transfer agreements with other companies" ".NET Web Api" {
            this -> po "Creates wallet deposit endpoint"
        }
        deleteConnectionInvitationsWorker = component "Delete Connection Invitations Worker" "Deletes expired connection invitations" ".NET BackgroundService"
        transferAgreementAutomation = component "Transfer Agreements Automation" "Transfers certificates within a given transfer agreement" ".NET BackgroundService" {
            this -> po "Transfers certificates"
        }
    }
    tDb = container "Transfer Storage" {
        description ""
        technology "Postgres SQL"

        tApi -> this "Saves and reads transfer agreement and connections data"
    }
    apiGateway -> connectionsApi "Forwards requests to"
    apiGateway -> transferAgreementsApi "Forwards requests to"
    connectionsApi -> tDb "Stores connections"
    transferAgreementsApi -> tDb "Stores transfer agreements"
    transferAgreementAutomation -> tDb "Reads transfer agreements"
    deleteConnectionInvitationsWorker -> tDb "Deletes connection invitations"
}
