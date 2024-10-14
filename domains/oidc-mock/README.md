# eo-oidc-mock

This is a mock of an OpenID Connect (OIDC) authorization server. When the user authorizes, a page is shown with a button for each user (or persona) to login as. More specifically, it is a mock to substitute the authorization server from Signaturgruppen.

## Endpoints and pages

The only supported flow is Authorization Code Flow. The endpoints and pages are relevant for implemented Authorization Code Flow and are listed in the order of the flow wrt. the authorization server:

- `.well-known/openid-configuration`: Implemented by [AuthController](Mock/Controllers/AuthController.cs). Returns a configuration mostly 1 to 1 with the one from Signaturgruppen.
- `.well-known/openid-configuration/jwks`: Implemented by [AuthController](Mock/Controllers/AuthController.cs). Returns the key used for signing the tokens as a JSON Web Key (JWK).
- `connect/authorize`: Implemented by [AuthController](Mock/Controllers/AuthController.cs) and will redirect the user to the sign in page
- `connect/signin`: Implemented by [Signin Razor page](Mock/Pages/Connect/Signin.cshtml) and shows list of user. When user selects a user, the page will redirect the browser to the redirect uri with the authorization code.
- `connect/token`: Implemented by [AuthController](Mock/Controllers/AuthController.cs). Receives the authorization code, client id and secret and if valid returns tokens
- `api/v1/session/logout`: Implemented by [AuthController](Mock/Controllers/AuthController.cs) and is used to logout the user in legacy auth domain.
- `connect/endsession`: Implemented by [AuthController](Mock/Controllers/AuthController.cs) and is used to logout the user.

## Points of interest

- The main reason for having a custom mock of Signaturgruppens authorization server is due to how the scope "userinfo_token" is handled. When the scope "userinfo_token" is used, then Signaturgruppens token endpoint will return a JWT called "userinfo_token". That token is used by the Auth-domain, so the mock must support that. This behaviour of adding "userinfo_token" to the scope resulting in an addition JWT in the token response is not supported by e.g. [OpenIddict](https://github.com/openiddict/openiddict-core). Hence, the decision was made to do a custom implementation.

## Development

When running the application in development mode, users are available along with a client (containing client id, secret and redirect uri). To start the authorization flow in development mode, navigate to https://localhost:7124/Connect/Authorize?client_id=energy-origin&redirect_uri=https://localhost:7124&response_type=code&scope=openid%20nemid%20mitid%20ssn%20userinfo_token&state=c6770cf5939c4db9bd293b189c2d0107&response_mode=query
diff
