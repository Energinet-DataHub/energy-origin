# Admin Portal

## Overview

The Admin Portal is the operator UI for Energy Track & Trace.
It provides controlled workflows for Organization Management, and metering-points management.
The portal does not own data, and never interfaces directly with our datastores;
All calls go through upstream subsystems that own the data.
Only workflows that modify data are documented with sequence diagrams.

## Workflows

### Add Organization to Whitelist

This is done by an Admin through the Admin Portal.
The Admin submits a form to add an organization to the whitelist.
A whitelist record is created if absent,
and it invalidates the organization’s accepted terms if present, and commits atomically.
Returns 201 Created on success; errors are surfaced to the portal for redirect and messaging.

```mermaid
sequenceDiagram
    actor Admin as Admin
    participant AdminPortal as Admin Portal
    participant Backend as Authorization Subsystem

    Admin->>AdminPortal: Submit "Add Organization to Whitelist" form
    note right of Admin: POST /WhitelistedOrganizations { Tin }
    AdminPortal->>Backend: POST /whitelisted-organizations { Tin }
    activate Backend

    Backend->>Backend: Begin transaction
    Backend->>Backend: Check if already whitelisted by Tin
    alt Not whitelisted
        Backend->>Backend: Create whitelist entry
    else Already whitelisted
        Backend->>Backend: No-op
    end
    Backend->>Backend: Find organization by Tin
    alt Organization found
        Backend->>Backend: Invalidate terms
    end
    Backend->>Backend: Commit

    Backend-->>AdminPortal: 201 Created { Tin }
    AdminPortal-->>Admin: Redirect to Index (success)

    alt Error occurs
        Backend-->>AdminPortal: Error response
        AdminPortal-->>Admin: Redirect to Index (error)
    end

    deactivate Backend
```

### Deactivate Organization / Remove from Whitelist

This is done by an Admin through the Admin Portal.
The Admin submits a form to remove an organization from the whitelist.
The Authorization service validates current status;
if Normal, it sets status to Deactivated, revokes terms and SP terms, and persists and publishes the change atomically.
Other states are rejected.

```mermaid
sequenceDiagram
    actor Admin
    participant AdminPortal as Admin Portal
    participant Authorization as Authorization Subsystem

Admin->>AdminPortal: POST /ett-admin-portal/WhitelistedOrganizations/remove
note right of Admin: application/x-www-form-urlencoded
activate AdminPortal
AdminPortal->>Authorization: DELETE api/authorization/admin-portal/whitelisted-organizations/{tin}
deactivate AdminPortal

    activate Authorization
    Authorization->>Authorization: Load & validate current status
    alt [Status == Normal]
Authorization->>Authorization: Set status = Deactivated<br>Revoke terms & SP terms<br>Persist & publish event atomically
    else [Status != Normal]
        Authorization-->>AdminPortal: Reject (invalid state)
    end
    deactivate Authorization
```
