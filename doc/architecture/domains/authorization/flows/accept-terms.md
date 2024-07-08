# Terms Acceptance Documentation

## Overview

The Terms Acceptance feature allows organizations to accept the latest terms and conditions.
When an organization accepts the terms, the Authorization database is updated, and an integration event is published
to RabbitMQ, using MassTransit's transactional outbox pattern.

## Accept terms flow:

```mermaid
sequenceDiagram
    actor User
    participant API as Terms Endpoint
    participant Handler as Command Handler
    participant DB as Database
    participant RabbitMQ as RabbitMQ Broker

    User->>API: POST /api/authorization/terms/accept
    activate API
    API->>API: Validate JWT and extract claims
    API->>Handler: Send AcceptTermsCommand
    activate Handler

    rect rgb(100, 100, 100)
        Note over Handler, DB: Database Interactions
        Handler->>DB: Begin transaction
        activate DB
        Handler->>DB: Get or create organization
        DB-->>Handler: Return organization
        Handler->>DB: Fetch latest terms
        DB-->>Handler: Return latest terms
        Handler->>DB: Update organization's terms acceptance
        DB-->>Handler: Confirm update
        Handler->>DB: Commit transaction
        DB-->>Handler: Confirm commit
        deactivate DB
    end

    rect rgb(100, 100, 100)
        note over Handler, RabbitMQ: Publish integration event
        Handler->>RabbitMQ: Send OrgAcceptedTerms message
    end

    Handler-->>API: Return true
    deactivate Handler

    API-->>User: 200 OK (Terms accepted successfully)
    deactivate API

    alt Terms Acceptance Fails
        activate Handler
        Handler-->>API: Return false
        deactivate Handler
        activate API
        API-->>User: 400 Bad Request (Failed to accept terms)
        deactivate API
    end
```

## Endpoint

`
POST /api/authorization/terms/accept
`

### Authorization

The endpoint requires authorization with the `B2CCvrClaim policy`.

This policy ensures that the user attempting to accept the terms is indeed affiliated with the organization,
they are accepting the terms on behalf of.

The policy is implemented as a custom authorization policy,
in the [EnergyOrigin.TokenValidation NuGet package](../../../../../libraries/dotnet/EnergyOrigin.TokenValidation/README.md).
