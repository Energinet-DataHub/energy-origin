# Claims

* Status: proposed
* Deciders: @CodeReaper, @duizer, @MartinSchmidt
* Date: 2022-11-29

---

## Context and Problem Statement

Once a user is logged in, the frontend needs to know additional information like:
- What roles the user has
- Which features to show
- Which capabilities the user has
- When the token expires

Note this need is very similiar to the needs of each domain where we are using JWTs with claims.

---

## Considered Options

* Separate API calls for user roles/features/capabilities
* JWT tokens

---

## Decision Outcome

We chose to use JWT tokens with scope claim for adherence to (RFC 8693)[https://www.rfc-editor.org/rfc/rfc8693.html]:
```jsonc
{
  "sub": "1234567890",
  "name": "John Doe",
  "iat": 1516239022,
  "exp": 1516240822,
  "scope": "certificates fun jokes",
  //...
}
```

## Rationale

JWTs is the simpler solution since the information about the user can be baked into the token and there is a lot of builtin support for JWTs.

### Positive Consequences

There is no need for a separate API for roles/features/capabilities.

The JWT will continue to work even if the auth domain is down.

The frontend will always be in-sync with what the user can do based on the scopes.

### Negative Consequences

If the backend requires information to be present in the JWT, the frontend (and externals) can read it too.
