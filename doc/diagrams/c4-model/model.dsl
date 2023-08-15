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
    taApi = container "Transfer Agreement API" {
        description ""
        technology ".NET Web Api"
        this -> po "Creates a Wallet Deposit Endpoint
    }

    taDatabase = container "Transfer Agreement Database" {
        description "Stores Transfer agreements"
        technology "PostgreSQL"
        taApi -> this "Saves and reads transfer agreements using"
    }

    taAutomation = container "Transfer Agreement Automation" {
        description "Transfer Certificates based on a Transfer Agreement"
        technology ".NET Background service"
        this -> taDatabase "Fetches all Transfer Agreements"
        this -> po "Transfer Certificates to the receivers Project Origin Wallet"
    }
}
