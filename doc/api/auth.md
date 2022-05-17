
# Auth domain

Documentation of the APIs on the Auth domain

---

## OIDC Login

Call to initiate the login flow.

### Request

```text
GET /api/auth/oidc/login
    ?return_url=https://example.com
    &fe_url=https://example.com
```

### Parameters

- return_url: The url to return to after login success.
- fe_url: The url for the auth frontend.

### Response

```json
{
    "next_url": "https://example.com"
}
```

---

## OIDC Login callback

Callback from the OIDC provider.

### Request

```
GET /api/auth/oidc/login/callback
    ?state=...
```

### Parameters

- state: OpenID Connect state object
- iss: Identifier for the issuer as an URL.
- code: Response type
- scope: OpenID Connect scopes ('openid', 'mitid', 'nemid', ...)
- error: Error response.
- error_hint: Text hint of the error.
- error_description: Text description of the error.

### Response

HTTP redirect 307

---

## OIDC Invalidate

Logout user on OIDC provider when user is in the onboarding flow,
if the user cancels or flow fails.


### Request

```
POST /api/auth/invalidate
```

### Parameters

\-

### Response

```json
{
    "success": true
}
```

## Logout

Used to logout the current user, includes back-channel logout to OIDC provider.

### Request

```
POST /api/auth/logout
```

### Parameters

\-

### Response

```json
{
    "success": true
}
```

## Profile

Gets the user profile for the current user.

***NOTICE*** to be superseded by [#context]

### Request

```
GET /api/auth/profile
```

### Parameters

\-

### Response

```json
{
    "success": true,
    "profile": {
        "id": "20316CDF-B31F-4DF1-92C1-41CD6F4A8DF0",
        "name": "John Doe",
        "company": null
    }
}
```


## Context

Gets context of the authorized user.

### Request

```
GET /api/auth/context
```

### Parameters

\-

### Response

```json
{
    "user": {
        "name": "John Doe",
    },
    "company": {
        "name": "Energinet DataHub",
    }
}
```

## Terms

TODO

## Accept Terms

TODO


## Forward auth

todo