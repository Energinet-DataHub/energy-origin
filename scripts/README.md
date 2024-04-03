# .NET Solution Upgrade dotnet libraries' NuGet Dependencies

This script is designed to automatically increment the patch version in `configuration.yaml` for .NET solutions that have had their dependencies updated using the `dotnet outdated` tool.

## How It Works

The script searches for all `.sln` (solution) files within the `libraries/dotnet` directory and its subdirectories. For each solution found, it runs `dotnet outdated -u` to update the project dependencies. If the command indicates that dependencies have been updated, the script then increments the patch version number in the corresponding `configuration.yaml` file located in the same directory as the `.sln` file.

## Prerequisites

To run this script, you must have the following prerequisites installed on your system:

- [.NET SDK](https://dotnet.microsoft.com/download) - Required to build and run .NET applications.
- [dotnet-outdated-tool](https://github.com/dotnet-outdated/dotnet-outdated) - A .NET global tool to identify outdated NuGet packages in your projects.

### Installing `dotnet-outdated-tool`

Installing `dotnet-outdated-tool` can be done using the following command:

```sh
dotnet tool install --global dotnet-outdated-tool
```

### Usage

To use the script, simply execute it from the terminal.
Ensure you're in the correct directory where the script is located, or provide the appropriate path to the script.
The script does not require any arguments to run:

```sh
./update_nugets_in_libraries.sh
```
