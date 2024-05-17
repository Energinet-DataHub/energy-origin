## Build Subsystems Workflow

### Overview

The Build Subsystems workflow is designed to streamline the continuous integration process by selectively building only the subsystems that have changed.

### Purpose

This workflow is invoked by other workflows, such as the CI Orchestrator, to detect changes in specific directories and trigger corresponding build processes. It enhances the modularity and scalability of the CI/CD pipeline by focusing on individual subsystems.

### How It Works

The workflow operates by detecting changes in predefined directories and triggering builds for the affected subsystems. This is accomplished through a series of jobs that set up the environment, detect changes, and build the necessary components.

### Inputs

The workflow accepts the following inputs:

- **dry-run**: This input controls whether the build should produce final artifacts or just perform a test run.
- **is-dependabot**: Indicates if the pull request is created by Dependabot. This input allows the workflow to apply specific logic or skip certain steps for Dependabot PRs.

### Jobs and Steps

#### 1. Setup Job

The `setup` job is responsible for detecting changes in specific directories. It sets outputs that determine which subsystems need to be built.

**Outputs**:
- `auth`, `authorization`, `certificates`, `measurements`, `oidc-mock`, `transfer`: Booleans indicating if changes were detected in the corresponding directories.

**Steps**:
- **Detect Auth**: Checks for changes in the `domains/auth/` directory.
- **Detect Authorization**: Checks for changes in the `domains/authorization/` directory.
- **Detect Certificates**: Checks for changes in the `domains/certificates/` directory.
- **Detect Measurements**: Checks for changes in the `domains/measurements/` directory.
- **Detect OIDC Mock**: Checks for changes in the `domains/oidc-mock/` directory.
- **Detect Transfer**: Checks for changes in the `domains/transfer/` directory.

#### 2. Build Jobs

The following jobs are conditionally executed based on the outputs from the `setup` job. Each job builds a specific subsystem if changes were detected.

- **Build Auth**: Builds the authentication subsystem.
- **Build Authorization**: Builds the authorization subsystem.
- **Build Certificates**: Builds the certificates subsystem.
- **Build Measurements**: Builds the measurements subsystem.
- **Build OIDC Mock**: Builds the OIDC mock subsystem.
- **Build Transfer**: Builds the transfer subsystem.

Each build job uses the corresponding workflow file (e.g., `build-auth.yaml`) and inherits secrets. The jobs accept the same inputs: `dry-run` and `is-dependabot`.

### How to Add a New Subsystem

To add a new subsystem to the CI/CD pipeline, follow these steps:

1. **Create a Directory**: Ensure the new subsystem has a dedicated directory under `domains/` (e.g., `domains/new-subsystem/`).

2. **Add Detection Step**:
    - Modify the `setup` job to include a new step for detecting changes in the new subsystem directory.
    - Use the `CodeReaper/find-diff-action@v3` action to check for changes. For example:
      ```yaml
      - name: detect new-subsystem
        id: new-subsystem
        uses: CodeReaper/find-diff-action@v3
        with:
          paths: domains/new-subsystem/
      ```
    - Add an output for the new subsystem:
      ```yaml
      outputs:
        new-subsystem: ${{ steps.new-subsystem.outputs.matches }}
      ```

3. **Create a Build Workflow**: Create a new workflow file (e.g., `build-new-subsystem.yaml`) to define the build process for the new subsystem.

4. **Add a Build Job**:
    - Add a new job in the Build Subsystems workflow to build the new subsystem if changes are detected. For example:
      ```yaml
      build-new-subsystem:
        needs: setup
        if: needs.setup.outputs.new-subsystem == 'true'
        uses: ./.github/workflows/build-new-subsystem.yaml
        secrets: inherit
        with:
          dry-run: ${{ inputs.dry-run }}
          is-dependabot: ${{ inputs.is-dependabot }}
      ```
5. **Add specific workflow file**: Create a new [specific workflow](./specific-subsystem-workflows.md) to define the build process for the new subsystem. Place this file in the `.github/workflows/` directory.
5. **Test the Integration**: Ensure the new setup and build jobs are correctly integrated by running the workflow and verifying that changes in the new subsystem directory trigger the appropriate build process.
