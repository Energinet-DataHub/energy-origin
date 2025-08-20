# Platform Architecture Specification

This document contains the overall architecture of the EnergyOrigin platform

## Intended readers and confidentiality

This document is intended for Architects and Developers
using and building the platform.

This document is public as it is a part of the OpenSource project
called EnergyOrigin.

---

## Architecture Overview

This chapter describes the background of our architecture, its purposes,
constraints and high level architecture.

Diagrams below are documented based on [C4 model]https://c4model.com)

### Motivation

As part of the Green transition in the energy sector Granular Certificates are
becoming a central part of the puzzle to solve the ever growing need to be able
to document the green and/or renewable aspect of energy.

The changes in how energy is handled and consumed is changing in an increasing
manner, so a platform that can handle these challenges is needed to enable the
green transition.

### Architectural Goals and Constraints

The goals of the architecture are:

- **Platform**: create an extendable platform where individual domains are
loosely coupled, to enable domains to be added, replaced or removed.

- **Isolated Domains**: the platform isolates groups of services into domains,
each domain can have a larger responsibility than a single service.
**No changes** required to other domains.

### System Business Requirements and Constraints

Business requirements:

- **OpenSource**: the platform must be OpenSource and a community created around it.
This will enable a shared benefit over time.

- **Avoid vendor lock-in**: The platform should not be limited by proprietary third-party
software to avoid vendor lock in.

- **Cloud Agnostic**: The platform should be able to run on any cloud or on-premise to not
limit it to specific users who only use one cloud provider.

---

## Design Requirements

- **Secure by design**: At every point during the design of the system,
security must be considered.

- **Privacy by design (GDPR)**: The platform must have built in support
to enable users to get their data and be deleted on request without manual steps.

- **Use existing IAM or external authentication**:
The platform must enable the use of OAuth2 and OIDC for user authentication
of normal users and OIDC to existing IAM for administrative users.

---

## Design Decisions

[See more here](adr/README.md)

---

## Test Strategy

All code must be sufficiently tested by unit and integrations tests,
so that the developers feel safe when deploying without any gates to production.

All manual tests will be regarded as tech-debt.

---

## High Level context diagram

Context diagram, high level overview of the system that will be part of the Danish deployment.

![Overview of the different systems integrating](diagrams/context.drawio.svg)

## Domains

[See more about the domains in the system](domains/README.md)
