# Dotnet - Build Action

### Overview

The `Dotnet - Build` action is designed to build a .NET project and create Docker images within a GitHub Actions workflow.

### Purpose

This action is used in the subsystem workflows as part of the CI/CD pipeline. It is responsible for building the .NET project and creating Docker images, which are then pushed to a container registry and deployed to the live environment.

### How It Works

The action runs through several steps to set up the environment, ensure the correct SDK version is used, build the project, and create the Docker image:

1. **Load - global.json**: Determines the directory of the provided solution file and checks if a `global.json` file is present. If found, it extracts the SDK and runtime versions specified in the file and sets them as environment variables.
2. **Overwrite - if given optional input**: If the `sdk-version` or `runtime-version` inputs are provided, it overwrites the versions from `global.json` with the provided versions.
3. **Fail - if versions are not present**: Ensures that both SDK and runtime versions are present. If not, the action fails.
4. **Fetch Dockerfile**: Downloads the [Dockerfile](https://github.com/Energinet-DataHub/.github/Dockerfile.simplified) used for building the Docker image.
5. **Resolve image version and name**: Uses the [docker-image-version@v2](https://github.com/Energinet-DataHub/.github/.github/blob/main/actions/docker-image-version/action.yaml) action, from acorn, to extract the image version, and name, from the configuration file.
6. **Verify migrations**: If migrations are provided, it verifies them.
7. **Build and push Docker image**: Uses the [docker-build-and-push@v2](https://github.com/Energinet-DataHub/.github/.github/blob/main/actions/docker-build-and-push/action.yaml) action, from acorn, to build the Docker image using the specified Dockerfile and pushes it to the container registry.
8. **Scan image**: uses the [docker-scan@v2](https://github.com/Energinet-DataHub/.github/.github/blob/main/actions/docker-scan/action.yaml) action, from acorn, to scan the Docker image for vulnerabilities.
9. **Upload Docker image tar**: Saves and uploads the Docker image as a tar file.

### Inputs

The action accepts the following inputs:

### `solution`

- **Description**: The path to a .NET solution (.sln) file.
- **Required**: Yes

### `project`

- **Description**: The path to a .NET project (.csproj) file.
- **Required**: Yes

### `configuration`

- **Description**: The path to a configuration file.
- **Required**: Yes

### `migrations`

- **Description**: The path to a migrations file, if this project uses one.
- **Required**: No
- **Default**: ""

### `sdk-version`

- **Description**: The complete SDK version in the format `x.y.zzz`.
- **Required**: No
- **Default**: ""

### `runtime-version`

- **Description**: The complete runtime version in the format `x.y.z`.
- **Required**: No
- **Default**: ""

### Example Usage

Below is an example of how to use the `Dotnet - Build` action within a workflow that tests, builds, and updates the environment for the `auth` subsystem.

```yaml
name: Build Auth

on:
  workflow_call:
    inputs:
      dry-run:
        description: "An indication of whether to commit/publish results"
        required: true
        type: string
      is-dependabot:
        description: "An indication of a dependabot pull request"
        required: true
        type: string

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: ./.github/actions/dotnet-test
        with:
          solution: domains/auth/Auth.sln

  build-api:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: ./.github/actions/dotnet-build
        with:
          solution: domains/auth/Auth.sln
          project: domains/auth/Auth.API/API/API.csproj
          configuration: domains/auth/Auth.API/configuration.yaml
          migrations: domains/auth/migrations/API.sql
          dry-run: ${{ inputs.dry-run }}

  update:
    runs-on: ubuntu-latest
    name: Update environment
    needs:
      - test
      - build-api
    concurrency: commits-base-environment
    if: ${{ inputs.is-dependabot == 'false' }}
    steps:
      - uses: actions/checkout@v4

      - name: Update environment
        uses: Energinet-DataHub/acorn-actions/actions/update-base-environment@v4
        with:
          configurations: |
            domains/auth/Auth.API/configuration.yaml
          dry_run: ${{ inputs.dry-run }}
          github-app-id: ${{ vars.ACORN_GHA_APP_ID }}
          github-app-private-key: ${{ secrets.ACORN_GHA_PRIVATE_KEY }}
          registry-push: ${{ inputs.dry-run != 'true' }}
```
