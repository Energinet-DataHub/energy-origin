# Datahub2 - API Mock
A helm chart which creates a Datahub2 API mock, which mocks Datahub endpoints. The helm chart uses a private docker image published at [github.com/Energinet-DataHub/energy-origin](https://github.com/Energinet-DataHub/energy-origin). This separation has been decided upon to make it possible to keep the Helm chart public, without exposing the actual datahub 2 interface.

## Adding a new endpoint
To add a new endpoint follow the walkthrough [here](https://github.com/Energinet-DataHub/eo-datahub2-api-mock#adding-new-endpoint).

After adding the new endpoint and a docker image has been created, update the docker tag within `./chart/values.yaml` to use the new version of the docker image.
