# Claims

* Status: proposed
* Deciders: @CodeReaper, @duizer
* Date: 2022-11-29

---

## Context and Problem Statement

Once a user is logged in, the frontend needs to know:
- What roles the user has
- Which features to show
- Which capabilities the user has

Note this need is very similiar to the needs of each domain where we are using JWTs with claims.

---

## Considered Options

* Separate API calls for user roles/features/capabilities
* Front/Back-facing JWT token

---

## Decision Outcome

We chose to use front-facing JWT tokens.

**Front-facing**: Frontend-oriented roles/features/capabilities
```jsonc
{
  "sub": "1234567890",
  "name": "John Doe",
  "iat": 1516239022,
  "opaque": "9ecacb93-36ea-411b-96f0-7a84dba41202",
  "features": "certificates fun jokes",
  "roles": "admin",
  "capabilities": "view"
  //...
}
```

**Back-facing**: Backend-oriented where more sensitive data is permissiable
```jsonc
{
  "sub": "1234567890",
  "name": "John Doe",
  "iat": 1516239022,
  "meters": "571369256606002442 57131300000000003"
  //...
}
```

## Rationale

We will not have to add and maintain a separate API for roles/features/capabilities.

The opaque token (ID) used to look up the (back-facing) JWT could just be a claim in the front-facing token.

There is no chance of the frontend becoming out-of-sync with the logged-in.
