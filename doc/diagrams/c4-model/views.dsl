# The 'views.dsl' file is intended as a mean for viewing and validating the model. It should
#   * Extend the base model and override the 'energyOrigin' software system

# The `model.dsl` file must contain the actual model, and is the piece that must
# be reusable and included in other Structurizr files like `views.dsl` and
# deployment diagram files.

workspace extends "https://raw.githubusercontent.com/Energinet-DataHub/opengeh-arch-diagrams/main/docs/diagrams/c4-model/dh-base-model.dsl" {

    model {
        #
        # Energy Origin (extends)
        #
        !ref energyOrigin {

            # IMPORTANT:
            # The order by which models are included is important for how the system-to-system relationships are specified.
            # A system-to-system relationship should be specified in the "client" of a "client->server" dependency, and
            # hence systems that doesn't depend on others, should be listed first.

            !include model.dsl
        }
    }

    views {
        systemContext energyOrigin "EnergyOriginSystemContext" {
            title "[System Context] Energy Origin"
            include *
            autoLayout
        }
        container energyOrigin "EnergyOriginContainers" {
            title "[Container Context] Energy Origin"
            include *
            autoLayout
        }

        # Specific area container views
        container energyOrigin "DataHubFacadeContainers" {
            title "[Container Context] DataHubFacade"
            include ->dataHubFacadeSubsystem->
            autoLayout
        }
        container energyOrigin "AuthContainers" {
            title "[Container Context] Auth"
            include ->authSubsystem-> dataHubFacadeApi->
            autoLayout
        }
        container energyOrigin "MeasurementsContainers" {
            title "[Container Context] Measurements"
            include ->measurementsSubsystem-> dataHubFacadeApi->
            autolayout
        }
        component measurementApi "MeasurementApiComponents" {
            title "[Component Context] Measurement API"
            include *
            autoLayout
        }
        container energyOrigin "CertificateContainers" {
            title "[Container Context] Certificates"
            include ->certificatesSubsystem->
            autoLayout
        }
        component certApi "CertificateApiComponents" {
            title "[Component Context] Certificate API"
            include *
            autoLayout
        }
        container energyOrigin "TransferContainers" {
            title "[Container Context] Transfer"
            include ->transferSubsystem->
            autoLayout
        }
        component transferApi "TransferApiComponents" {
            title "[Component Context] Transfer API"
            include *
            autoLayout
        }

        styles {
            element "MockingComponent" {
                background #ffbb55
                color #ffffff
            }
        }
    }
}
