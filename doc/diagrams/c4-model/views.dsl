# The 'views.dsl' file is intended as a mean for viewing and validating the model
# in the domain repository. It should
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
            # The order by which models are included is important for how the domain-to-domain relationships are specified.
            # A domain-to-domain relationship should be specified in the "client" of a "client->server" dependency, and
            # hence domains that doesn't depend on others, should be listed first.

            !include model.dsl
        }
    }


    views {
        systemContext energyOrigin "SystemContextEO" {
            title "[System Context] Energy Origin"
            include *
            autoLayout
        }
        container energyOrigin "ContainerContextEO" {
            title "[Container Context] Energy Origin"
            include *
            autoLayout
        }

        # Specific area container views
        container energyOrigin "DataSync" {
            title "[Container Context] DataSync"
            include ->dataSyncDomain->
            autoLayout
        }
        container energyOrigin "Auth" {
            title "[Container Context] Auth"
            include ->authDomain-> dataSyncApi->
            autoLayout
        }
        container energyOrigin "Measurements" {
            title "[Container Context] Measurements"
            include ->measurementsDomain-> dataSyncApi->
            autolayout
        }
        container energyOrigin "Certificate" {
            title "[Container Context] Certificates"
            include ->certificatesDomain-> dataSyncApi->
            autoLayout
        }
        component certApi "CertificateApiComponents" {
            title "[Component Context] Certificate API"
            include *
            autoLayout
        }
        container energyOrigin "Transfer" {
            title "[Container Context] Transfer"
            include ->transferDomain->
            autoLayout
        }
        component tApi "TransferApiComponents" {
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
