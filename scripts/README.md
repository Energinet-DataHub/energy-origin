# Upgrade .NET NuGet Dependencies

These scripts are designed to automatically, upgrade NuGet dependencies. There are 2 scripts in total:

- `subsystems_update_nugets.sh` - This script will update the NuGet dependencies in all .NET solutions found in the `domains` directory and its subdirectories.
- `update_nugets_in_libraries.sh` - This script will update the NuGet dependencies in all .NET solutions found in the `libraries/dotnet` directory and its subdirectories. It also increments the patch versions in `configuration.yaml` files, for the solutions which were updated using the `dotnet outdated` tool.

## How It Works

Each script searches for all `.sln` (solution) files within either the `libraries/dotnet` or the `domains` directory and its subdirectories. For each solution found, it runs `dotnet outdated -u` to update the project dependencies. If the command indicates that dependencies have been updated, the script then increments the patch version number in the corresponding `configuration.yaml` file located in the same directory as the `.sln` file, if applicable.

## Prerequisites

To run the scripts, you must have the following prerequisites installed on your system:

- [.NET SDK](https://dotnet.microsoft.com/download) - Required to build and run .NET applications.
- [dotnet-outdated-tool](https://github.com/dotnet-outdated/dotnet-outdated) - A .NET global tool to identify outdated NuGet packages in your projects.

### Installing `dotnet-outdated-tool`

Installing `dotnet-outdated-tool` can be done using the following command:

```sh
dotnet tool install --global dotnet-outdated-tool
```

### Usage

To use the scripts, simply execute one of them from the terminal.
Ensure you're in the correct directory where the script is located, or provide the appropriate path to the script.
The scripts does not require any arguments to run:

```sh
./update_nugets_in_libraries.sh
```

or

```sh
./subsystems_update_nugets.sh
```
