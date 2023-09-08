# TODO all diagrams should be revised to resemble Energy Origin as-is

apiGateway = container "API Gateway" {
    description "Routes requests to services and forwards authentication requests"
    technology "Traefik"
}

authDomain = group "Auth Domain" {
    authApi = container "Auth Web Api" {
        description "API For authentication and authorization"

        this -> mitId "Executes UIDC callbacks"
        apiGateway -> this "Forwards requests to"
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
    }
    certRabbitMq = container "Certificate Message Broker" {
        description ""
        technology "RabbitMQ"

        certApi -> this "Produces and consumes messages using"
    }
    certEventStore = container "Certificate Storage" {
        description ""
        technology "EventStore"

        certApi -> this "Saves and reads certificate models using"
    }
}

emissionsDomain = group "Emissions Domain" {

}

measurementsDomain = group "Measurements Domain" {

}

transferAgreementsDomain = group "Transfer Agreements Domain" {
    taApi = container "Transfer Agreement API" "" ".NET Web Api" {
        connectionsApi = component "Connections Api" "Allows users to see connections of their company." ".NET Web Api"
        transferAgreementsApi = component "Transfer Agreements Api" "Allows users to create transfer agreements with other companies" ".NET Web Api" {
            this -> po "Creates wallet deposit endpoint"
        }
        deleteConnectionInvitationsWorker = component "Delete Connection Invitations Worker" "Deletes expired connection invitations" ".NET BackgroundService"
        transferAgreementAutomation = component "Transfer Agreements Automation" "Transfers certificates within a given transfer agreement" ".NET BackgroundService" {
            this -> po "Transfers certificates"
        }
    }
    taDb = container "Transfer Agreement Storage" {
        description ""
        technology "Postgres SQL"

        taApi -> this "Saves and reads transfer agreement and connections data"
    }
    apiGateway -> connectionsApi "Forwards requests to"
    apiGateway -> transferAgreementsApi "Forwards requests to"
    connectionsApi -> taDb "Stores connections"
    transferAgreementsApi -> taDb "Stores transfer agreements"
    transferAgreementAutomation -> taDb "Reads transfer agreements"
    deleteConnectionInvitationsWorker -> taDb "Deletes connection-invitations"
}
