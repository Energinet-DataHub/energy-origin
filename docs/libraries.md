# Shared Libraries Overview

This document summarizes the shared libraries and utilities available in the `libraries/` directory. These libraries provide cross-cutting functionality and are used by multiple domains throughout the project.

## Structure

- **libraries/dotnet/**
  - **EnergyOrigin.ActivityLog/**: Provides activity logging functionality for tracking actions and events across services.
  - **EnergyOrigin.IntegrationEvents/**: Handles integration event publishing and consumption for inter-service communication.
  - **EnergyOrigin.TokenValidation/**: Contains logic for validating authentication tokens.
  - **EnergyOriginAuthorization/**: Provides shared authorization logic and helpers.
  - **EnergyOriginDateTimeExtension/**: Utility extensions for date and time handling.
  - **EnergyOriginEventStore/**: Implements event sourcing and event storage mechanisms.
- **libraries/docker/**
  - Contains Docker-related utilities and static files for containerized environments.

## Usage

Each library is designed to be reusable and is referenced by multiple domains. For more details, see the README files within each library folder or the NuGet package documentation.

---

For more information, refer to the root README.md or the documentation within each library.

