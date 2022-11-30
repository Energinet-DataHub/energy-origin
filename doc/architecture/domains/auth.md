# Auth Domain

## Container diagram

![Container diagram](../diagrams/auth.container.drawio.svg)

## Old endpoints

There is a description of the current endpoints available at [Auth API](../../api/auth.md).

## Tokens

It is important to understand there is one token meant for outside the cluster and one meant for inside the cluster.
Both these tokens are made by the Auth domain. The back-facing tokens relays user details and related information to each domain,
so they never has to ask for additional information. The front-facing tokens relays user settings to the frontend.

**Front-facing token**: Frontend-oriented roles/features/capabilities
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

**Back-facing token**: Backend-oriented where more sensitive data is permissiable
```jsonc
{
  "sub": "1234567890",
  "name": "John Doe",
  "iat": 1516239022,
  "meters": "571369256606002442 57131300000000003"
  //...
}
```

## Terms

Terms are defined in the base environment. These will be used both in the frontend domain and the auth domain.

The frontend domain will present the HTML and to do so will have the latest terms markdown convert into HTML and placed in an agreed upon location.

The auth domain will be configured with the latest version and can check if a user has accepted the latest version or not.
Based on this the claim relating to terms for the front-facing token can be calculated.

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
- cookie: Authentication={Front-facing token}

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
- X-Updated-Authentication: Bearer {Back-facing token}
