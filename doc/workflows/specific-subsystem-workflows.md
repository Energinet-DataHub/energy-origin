## Specific Workflows for Each Subsystem

### Overview

The subsystem workflows are designed to manage the build, test, and deployment processes for individual subsystems within our monorepo. These workflows are invoked by the "Build Subsystems" workflow and ensure that each subsystem, represented by a .NET solution, is properly handled based on changes detected in their respective directories.

### Purpose

Each subsystem workflow automates the test, build, and deployment process for a specific .NET solution and its associated projects.

### How It Works

Each workflow for a subsystem follows a standardized process:

1. **Test**: The entire .NET solution is tested to ensure the integrity of the solution's codebase using a custom [dotnet-test](./dotnet-test.md) action.
2. **Build**: Docker images are built for each project within the .NET solution using a standardized [Dockerfile](https://github.com/Energinet-DataHub/acorn-actions/Dockerfile.simplified) provided by Ratatosk, and a custom [dotnet-build](./dotnet-build.md) action.
3. **Update**: If all tests pass and the images are built successfully, the workflow will:
    - Push the newly built images to the GitHub Container Registry (ghcr.io).
    - Update the Infrastructure as Code (IaC) repository, `eo-base`, with the names of the latest images. This allows ArgoCD to pull the updated images from the registry.

### Inputs

The workflow accepts the following inputs, passed from the invoking workflow:

- **dry-run**: Indicates whether to commit or publish the results. This input controls whether the build should produce final artifacts or just perform a test run.
- **is-dependabot**: Indicates if the pull request is created by Dependabot. This input allows the workflow to apply specific logic or skip certain steps for Dependabot PRs.

### Jobs and Steps

#### 1. Test Job

The `test` job runs tests on the .NET solution to ensure the code is functioning correctly. It runs on the `ubuntu-latest` runner.

**Steps**:
- **Checkout Code**: Uses the [actions/checkout@v4](https://github.com/actions/checkout) action to check out the repository.
- **Run Tests**: Uses a custom [dotnet-test](./dotnet-test.md) action to run tests on the specified .NET solution.

#### 2. Build Jobs

These jobs build Docker images for specific projects within the .NET solution. Each job runs on the `ubuntu-latest` runner and uses a custom [dotnet-build](./dotnet-build.md) action.

**Example Build Job for a Project**:

**Steps**:
- **Checkout Code**: Uses the [actions/checkout@v4](https://github.com/actions/checkout) action to check out the repository.
- **Build Project**: Uses a custom [dotnet-build](./dotnet-build.md) action to build the project and create a Docker image.

#### 3. Update Environment Job

The `update` job updates the environment with the new build artifacts. This job runs on the `ubuntu-latest` runner and depends on the successful completion of all test and build jobs.

**Steps**:
- **Checkout Code**: Uses the [actions/checkout@v4](https://github.com/actions/checkout) action to check out the repository.
- **Upload Images**: Pushes the newly built Docker images to the GitHub Container Registry (ghcr.io).
- **Update IaC Repository**: Updates the `eo-base` repository with the names of the latest images, enabling ArgoCD to deploy the new images.

### How to Add a New Subsystem Workflow

To add a new subsystem workflow, follow these steps:

1. **Create a Directory**: Ensure the new subsystem has a dedicated directory under `domains/` (e.g., `domains/new-subsystem/`).

2. **Create a Workflow File**: Create a new workflow file (e.g., `build-new-subsystem.yaml`) to define the build process for the new subsystem. Place this file in the `.github/workflows/` directory.

3. **Define the Jobs**:

    - **Test Job**: Add a job to run tests on the .NET solution:
```yaml
     jobs:
       test:
         name: Test New Subsystem
         runs-on: ubuntu-latest
         steps:
           - uses: actions/checkout@v4
           - uses: ./.github/actions/dotnet-test
             with:
               solution: domains/new-subsystem/NewSubsystem.sln
```
- **Build Jobs**: Add jobs to build Docker images for each project within the .NET solution.
```yaml
     jobs:
       build-project1:
         name: Build Project 1
         runs-on: ubuntu-latest
         steps:
             - uses: actions/checkout@v4
             - uses: ./.github/actions/dotnet-build
               with:
                 solution: domains/new-subsystem/NewSubsystem.sln # Path to the .NET solution
                 project: domains/new-subsystem/Project1/Project1.csproj # Path to the project file
                 configuration: domains/new-subsystem/Project1/configuration.yaml # Configuration file for the project
                 migrations: domains/new-subsystem/migrations/Project1.sql # Migration script for the project, if applicable
                 dry-run: ${{ inputs.dry-run }} # Dry run flag
```
- **Update Environment Job**: Add a job to update the environment with the new build artifacts.
```yaml
      jobs:
        update-transfer:
          runs-on: ubuntu-latest
          name: Update environment
          needs:
            - test # Ensure tests have passed
            - build-project1 # Ensure project 1 is built
          concurrency: commits-base-environment
          if: ${{ inputs.is-dependabot == 'false' }}
          steps:
            - uses: actions/checkout@v4

            - name: Update environment
              uses: Energinet-DataHub/acorn-actions/actions/update-base-environment@v4
              with:
                configurations: | # Configuration files for each project
                  domains/new-subsystem/Project1.API/configuration.yaml
                dry_run: ${{ inputs.dry-run }}
                github-app-id: ${{ vars.ACORN_GHA_APP_ID }}
                github-app-private-key: ${{ secrets.ACORN_GHA_PRIVATE_KEY }}
                registry-push: ${{ inputs.dry-run != 'true' }}
```

NOTE: Adding Additional Build Jobs for other projects within the subsystem follows the same pattern as the `build-project1` job.

4. **Integrate with Build Subsystems Workflow**: Ensure the new workflow can be invoked by the [build-subsystems workflow](./build-subsystems.md) by adding the necessary steps and outputs to detect changes in the new subsystem directory.

5. **Test the Integration**: Ensure the new workflow is correctly integrated by running the Build Subsystems workflow and verifying that changes in the new subsystem directory trigger the appropriate build process.
