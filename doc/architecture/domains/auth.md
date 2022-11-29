# Auth Domain

## Container diagram

![Container diagram](../diagrams/auth.container.drawio.svg)

## Old endpoints

There is a description of the current endpoints available at [Auth API](../../api/auth.md).

## Tokens

It is important to understand there is one token meant for outside the cluster and one meant for inside the cluster.
Both these tokens are made by the Auth domain. The inner tokens relays user details and related information to each domain,
so they never has to ask for additional information. The outer tokens relays user settings to the frontend.

**Outer token**: Frontend-oriented roles/features/capabilities
```jsonc
{
  "sub": "1234567890",
  "name": "John Doe",
  "iat": 1516239022,
  "features": "certificates fun jokes",
  "roles": "admin",
  "capabilities": "view"
  //...
}
```

**Inner token**: Backend-oriented where more sensitive data is permissiable
```jsonc
{
  "sub": "1234567890",
  "name": "John Doe",
  "iat": 1516239022,
  "meters": "571369256606002442 57131300000000003"
  //...
}
```

## New endpoints

---

### Login

Starts the login flow with OIDC provider.

#### Request

```text
GET /api/auth/login
```

#### Response

HTTP 307 redirect

---

### Login Callback

Handle callback from OIDC provider by redirecting to:
- landing page
- login failure page with error code

#### Request

```text
GET /api/auth/oidc/callback
```

#### Query Parameters

- state: State provided when starting flow
- code: Code for retrieving tokens
- error: Error response
- error_description: Text description of the error

#### Response

HTTP 200 OK with [meta refresh](https://stackoverflow.com/a/64216367/190599) with header:
- cooke: Authentication={outer token}

---

### Logout

Starts the logout flow with OIDC provider.

#### Request

```text
GET /api/auth/logout
```

#### Response

HTTP 307 redirect

---

### Accept terms

Stores an accept of specific terms for a user

#### Request

```text
PUT /api/auth/terms/accept
```

#### Body

```json
{
    "version": 3
}
```

#### Response

HTTP 204 No content

---

### Forward auth

[ForwardAuth endpoint](https://doc.traefik.io/traefik/v2.0/middlewares/forwardauth/) for Træfik.

#### Request

```text
GET /api/auth/token/forward-auth
```

#### Response

HTTP 200 OK with header:
- Authentication: Bearer {inner token}

## Mising Coordination

How do we inform frontend when regenerating tokens?

How do we provide the frontend with latest terms?

Define currently required claims for frontend, like terms-not-accepted etc.
