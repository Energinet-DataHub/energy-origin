# System Architecture Overview

This document provides a high-level overview of the Energy Origin system architecture, including the main domains, their interactions, and key integration points.

## Architecture Summary

Energy Origin is organized into multiple domains, each responsible for a specific business capability (e.g., transfer, measurements, certificates, authorization, authentication, admin portal). Each domain typically exposes its own API and may include background workers, automation, and supporting utilities.

### Key Domains
- **Transfer**: Manages transfer agreements and automation.
- **Measurements**: Handles measurement data and integration with external data hubs.
- **Certificates**: Manages digital certificates and related queries.
- **Authorization**: Enforces authorization policies and proxies requests.
- **Auth**: Provides authentication services.
- **Admin Portal**: Web interface for administrative tasks.
- **OIDC Mock**: Simulates OIDC authentication for development/testing.
- **HTML PDF Generator**: Converts HTML to PDF via a web service.
- **Redoc**: Serves API documentation.

### Shared Libraries
Common functionality (logging, eventing, token validation, etc.) is provided by shared libraries in the `libraries/` directory.

### Integration & Communication
Domains interact via REST APIs, shared libraries, and integration events. Event-driven communication is facilitated by the `EnergyOrigin.IntegrationEvents` library.

### Deployment & Infrastructure
The system supports containerized deployment using Docker. CI/CD workflows and environment setup are documented in the `doc/workflows/` and root README.md.

## Further Reading
- [Detailed Architecture Docs](../doc/architecture/README.md)
- [Domain-Specific Architecture](../doc/architecture/domains/)
- [API Conventions](../doc/api/conventions.md)
- [CI/CD Workflows](../doc/workflows/)

---

For more details, see the linked documentation or explore the codebase.

