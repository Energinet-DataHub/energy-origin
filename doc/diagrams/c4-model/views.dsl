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
        systemContext energyOrigin "SystemContextEO" {
            title "[System Context] Energy Origin"
            include *
            autoLayout
        }
        container energyOrigin "ContainerEO" {
            title "[Container] Energy Origin"
            include *
            autoLayout
        }

        # Specific subsystem container/component views
        container energyOrigin "DataSync" {
            title "[Container] DataSync"
            include ->dataSyncSubsystem->
            autoLayout
        }
        container energyOrigin "Auth" {
            title "[Container] Auth"
            include ->authSubsystem-> dataSyncApi->
            autoLayout
        }
        container energyOrigin "Measurements" {
            title "[Container] Measurements"
            include ->measurementsSubsystem-> dataSyncApi->
            autolayout
        }
        container energyOrigin "Emissions" {
            title "[Container] Emissions"
            include ->emissionsSubsystem-> dataSyncApi->
            autolayout
        }
        container energyOrigin "Certificate" {
            title "[Container] Certificates"
            include ->certificatesSubsystem-> dataSyncApi->
            autoLayout
        }
        component certApi "CertificateApiComponents" {
            title "[Component] Certificate API"
            include *
            autoLayout
        }
        container energyOrigin "Transfer" {
            title "[Container] Transfer"
            include ->transferSubsystem->
            autoLayout
        }
        component transferApi "TransferApiComponents" {
            title "[Component] Transfer API"
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
