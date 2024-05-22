## Dotnet - Test Action

### Overview

The `Dotnet - Test` action is designed to run .NET tests for a specified solution file within a GitHub Actions workflow. This action ensures that the solution adheres to the required .NET SDK version and executes the tests, providing an automated and consistent way to validate your .NET codebase.

### Purpose

This action is used in the subsystem workflows as part of the CI/CD pipeline. It helps catch issues early by running tests before the build and deployment stages.

### How It Works

The action runs through several steps to set up the environment, ensure the correct SDK version is used, and execute the tests:

1. **Load - global.json**: Determines the directory of the provided solution file and checks if a `global.json` file is present. If found, it extracts the SDK version specified in the file and sets it as an environment variable.
2. **Overwrite - if given optional input**: If the `sdk-version` input is provided, it overwrites the SDK version from `global.json` with the provided version.
3. **Fail - if sdk-version is not present**: Ensures that the SDK version is present. If not, the action fails.
4. **Test**: Uses the `dotnet-validate-solution` action to run the tests on the solution. The action validates the solution and pins the version if necessary.

### Inputs

The action accepts the following inputs:

- **solution**:
    - **Description**: The path to a .NET solution (.sln) file.
    - **Required**: Yes

- **sdk-version**:
    - **Description**: The complete SDK version in the format `x.y.zzz`.
    - **Required**: No
    - **Default**: ""

### Example Workflow

Below is an example of how to use the `Dotnet - Test` action within a workflow that tests, builds, and updates the environment for a specific subsystem.


```yaml
  name: Build New-subsystem

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
            solution: domains/new-subsystem/New-subsystem.sln
```
