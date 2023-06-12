# TODO all diagrams should be revised to resemble Energy Origin as-is

# TODO MitId should be a software system in our C4 base system landscape
group "SignaturGruppen" {
    mitId = container "MitID" {
        description "SignaturGruppen is the OIDC provider for users"
        tags "Out of focus"
    }
}

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
    
}