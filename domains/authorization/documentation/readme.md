# Azure B2C 

Link to description of flow by Microsoft [Client Credentials Grant](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-client-creds-grant-flow)


``` Mermaid 
C4Context
  title Authentication & Authorization 
  Person(user, "MitID Erhverv user")
  Enterprise_Boundary(eoBoundary, "Energy Origin") {
    System(WEB, "Energy Origin WEB", "")
    System(API, "Energy Origin API", "HTTP API") 
    System_Boundary(AUTH, "Authentication") {
      System(B2C, "Azure AD B2C", "Energy Origin Identity Provider")
    }
  }
  System_Ext(MitID, "MitID", "Signaturgruppen MitID provider")
  Rel(user, WEB, "Access resources and provide consent", "OpenID Connect")
  Rel(user, MitID, "Logs in", "")
  Rel(WEB, B2C, "Delegates log in ", "OpenID Connect")
  Rel(WEB, API, "Uses HTTP APIs", "HTTPS")
  Rel(B2C, MitID, "Delegates authentication of MitID users", "OpenID Connect")
```

## Grant consent to 3rd party

Grant consent for 3rd party client to access and administer data in Energy Origin.

``` Mermaid 
sequenceDiagram
    actor User
    participant ClientApp as Client Application
    participant EO as Energy Origin
    participant B2C as B2C
    participant MitID as MitID
    participant Auth as Authorization

    User->>ClientApp: Grant consent
    ClientApp->>EO: Redirect to EO    

    rect rgb(50, 50, 50)
        note over EO, MitID: Authorization Code Flow
        EO->>B2C: Redirect to B2C
        B2C->>MitID: Redirect to MitID
        User->>MitID: Login
        MitID->>B2C: Successfull login (tokens)
        B2C->>EO: Successfull login (tokens)    
    end

    activate EO
    EO->>EO: Redirect user to consent page

    User->>EO: User accepts consent
    EO->>Auth: Update consent (link CVR and clientId)
    deactivate EO
```

## 3rd party access

### Register B2C as client

Azure B2C itself will have to be registered as a client. This allows B2C to obtain a token for itself and call the EO authorization API. 

![New App Registration](/images/new_app_registration.png)

Provide the name `self` for the app, and register by clicking `Register`.

Afterwards change settings in the manifest file. Make sure the following two settings are present: `"signInAudience": "AzureADMyOrg"` and `"accessTokenAcceptedVersion": 2`.

Add a new client secret to the app registration.

![New Client Secret](/images/new_client_secret.png)

Make sure to copy the secret value and store it somewhere safe. Use client id and client secret to configure client_credentials custom policy.

Call Energy Origin API as 3rd party client on behalf of organization.

``` Mermaid 
sequenceDiagram
    participant ClientApp as Client Backend
    participant B2C as B2C
    participant Auth as Authorization
    participant Cert as Certification API

    rect rgb(50, 50, 50)
        note over ClientApp,Auth: Client Credentials Flow
        ClientApp->>B2C: Client Credentials flow
        activate B2C
        B2C->>B2C: Client credentials flow (B2C as client)
        B2C->>Auth: Get authorization (B2C access token)
        activate Auth
        Auth->>B2C: Authorization (CVR list)
        deactivate Auth
        B2C->>ClientApp: Token
        deactivate B2C
    end

    ClientApp->>Cert: API call (token)
    ClientApp->>Cert: ...
    ClientApp->>Cert: API call (token)
```

## Links

 [MitID test user tool](https://pp.mitid.dk/test-tool/frontend/#/view-identity)

[MitID admin tool](https://pp.netseidbroker.dk/admin#/clients/e9d55f7d-03b6-4ec8-be83-f2804f32f9d0)

[MitID test clients](https://broker.signaturgruppen.dk/en/technical-documentation/open-oidc-clients)


[Confluence: MitID test brugere](https://energinet.atlassian.net/wiki/spaces/ElOverblik/pages/678133811)

[Confluence: Driftinfo hos Netsbroker](https://energinet.atlassian.net/wiki/spaces/ElOverblik/pages/307232769)

## Test with ngrok on localhost

Set up ncat to listen and dump all traffic on port 9090 on localhost

```nc -k -l 9090```

Set up ngrok to forward traffic to netcat

```ngrok http 9090```
