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
        container energyOrigin "Certificate" {
            title "[Container Context] Certificates"
            include ->certificatesDomain->
            autoLayout
        }
        container energyOrigin "Auth" {
            title "[Container Context] Auth"
            include ->authDomain->
            autoLayout
        }
        container energyOrigin "TransferAgreement" {
            title "[Container Context] Transfer Agreement"
            include ->transferAgreementsDomain->
            autoLayout
        }
}
