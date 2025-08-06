# Guide for configuring B2C tenant for Energy Origin

## Introduction

This document contains a step-by-step guide on how to configure the ETT B2C production tenant in Azure. This is needed in case we need to configure Azure B2C again, if someone deletes it by accident. Can also be used by other teams, who needs a similar setup.

### Prerequisites

The following prerequiresites must be addressed before starting the configuration.

- __Existing tenant:__ A B2C instance must be available, the current instance was created by T/I by request.
- __Invitation:__ Users must be invited to the new B2C tenant. After accepting the invitation, the user may use the switch workspace option in Azure to access the tenant.
- __Access rights:__ Users must have sufficient access rights to configure the tenant. (Currently we use the `global administrator` role)

### App registrations

#### B2C app registration

Create registration for `ett-internal-authorization-b2c` client. This client will be used by custom policies to make requests to `Authorization` service.

Navigate to `App registrations`.

![App registrations](images/App_registrations.png)

Click `New registration`.

![New app registration](images/App_registrations_new.png)

Enter `ett-internal-authorization-b2c` in `Name`. Uncheck `Grant admin consent to openid and offline_access permission` checkbox. Finally click `Register`.

![New b2c app registration](images/App_registration_b2c.png)

The client id of the newly generated app registration is needed to configure the Authorization sub system to accept tokens when getting consent and performing admin requests. The client id when using client-credentials outside of the custom flows, is the object id of the underlying enterprise application. Navigate to the newly created app registration, and click the mananaged app link.

![New b2c app registration enterprise app](images/App_registration_b2c_enterprise_app.png)

The object id will be used as subject in access tokens issued with client credentials. This id must be configured in the Authorization sub system.

![New b2c app registration enterprise app oid](images/App_registration_b2c_enterprise_app_oid.png)

##### Add secret

Navigate to newly created app registration. Values for tenant-id and client-id can be found on this page. These values are needed later. Click `Add a certificate or secret`.

![New b2c certificate](images/App_registration_b2c_certificate.png)

Click `New client secret`.

![New b2c certificate](images/App_registration_b2c_certificate_new.png)

Leave `Description` and `Expires` fields empty. Click `Add`.  __?!? Decide on expires value__

![New b2c certificate](images/App_registration_b2c_certificate_new_props.png)

The generated secret value will not be available later. Make sure to copy the value now and store it somewhere secure.

![New b2c certificate](images/App_registration_b2c_certificate_secret_value.png)

##### Application id

Add an application id to the newly registered application. Click `Add an Application ID URI`.

![New b2c certificate](images/App_registration_b2c_application_id.png)

Click `Edit` and enter `energy-origin` in the application id field. Click `Save`.

![New b2c certificate](images/App_registration_b2c_application_id_props.png)

Get application id url (used in scope when performing a client credentials flow).

![New b2c certificate](images/App_registration_b2c_application_id_get.png)

##### Test client credentials

Test client-credentials flow is working for the newly created app registration. The following REST script can be modified to perform the test. Fill in the missing values aquired previously in this guide.

```rest
@grantType = client_credentials
@clientId = <fill-in>
@clientSecret = <fill-in>
@scope = <fill-in>
@tenantId = <fill-in>
###

# @name tokenResponse
POST https://login.microsoftonline.com/{{tenantId}}/oauth2/v2.0/token HTTP/1.1
Content-Type: application/x-www-form-urlencoded

grant_type={{grantType}}
&client_id={{clientId}}
&client_secret={{clientSecret}}
&scope={{scope}}
```

#### Energy Track and Trace app registration

Create an app registration for Energy Track & Trace frontend. This app registration will be used by the frontend application to authenticate users. Navigate to `App registrations`  and click `New registration`.

![New app registration](images/App_registrations_new.png)

Provide name `ett-frontend`. Uncheck `Grant admin consent to openid and offline_access permission` checkbox. Finally click `Register`.

![New app registration](images/App_registration_frontend.png)

Click `Add a redirect URI`.

![New app registration](images/App_registration_frontend_redirect_urls.png)

Choose `Single-page application` as platform.

![New app registration](images/App_registration_frontend_redirect_urls_new.png)

Add the Energy Track & Trace URL `https://energytrackandtrace.dk/da/callback` as redirect URL. Click `Configure`.

![New app registration](images/App_registration_frontend_redirect_urls_new_props.png)

Add a second redirect URL `https://energytrackandtrace.dk/en/callback` to the list, and click `Save`.

![New app registration](images/App_registration_frontend_redirect_urls_new_2nd.png)

Add API permission to allow user sign-in for this specific app. Navigate to `App registration` and `API permissions`.

Add permissions to app registration.

![Frontend app registration permissions](images/App_registration_frontend_permissions_add.gif)

Grant admin consent to permissions.

![Frontend app registration permissions](images/App_registration_frontend_permissions_admin_consent.png)

#### Authorization app registration

Create the app registration `ett-internal-authorization-secrets`. This client will be used by the Authorization subsystem to call the Microsoft Graph API.

Specificially it will be used to get/create/delete secrets in app registrations for third-party clients.

Navigate to `App registrations`.

![App registrations](images/App_registrations.png)

Click `New registration`.

![New app registration](images/App_registrations_new.png)

Enter `ett-internal-authorization-secrets` in `Name`.

Under `Supported account types`, choose `Accounts in this organizational directory only (Single tenant)`.

Uncheck `Grant admin consent to openid and offline_access permission` checkbox. Finally click `Register`.

![App registrations](images/authorization/appregistration-new.png)

##### Add secret

Navigate to newly created app registration. Values for tenant-id and client-id can be found on this page. These values are needed later. Click `Add a certificate or secret`.

![App registration created](images/authorization/appregistration-created.png)

Click `New client secret`.

![New secret](images/authorization/appregistration-newsecret.png)

Give the secret a `Description` and set `Expires` to two years in the future (maximum expiration). Click `Add`.

![Secret setup](images/authorization/appregistration-secret.png)

The generated secret value will not be available later. Make sure to copy the value now and store it somewhere secure.

![Secret created](images/authorization/appregistration-secretcreated.png)

##### Add API permission

Navigate to `API permissions`.

![Navigate to API permissions](images/authorization/appregistration-permissions.png)

Click `Add a permission` and select `Microsoft Graph`.

![Add graph permissions](images/authorization/appregistration-permissionsgraph.png)

Click `Application permissions` and add the permission `Application.ReadWrite.All`.

![Application permissions](images/authorization/appregistration-permissionsapp.png)

Grant admin consent to the newly added permission.

![Navigate to API permissions](images/authorization/appregistration-permissionsconsent.png)

##### Test client credentials

Test that it's possible to get an access-token that can be used for the Microsoft Graph API. The following REST script can be modified to perform the test. Fill in the missing values aquired previously in this guide.

```rest
@grantType = client_credentials
@clientId = <fill-in>
@clientSecret = <fill-in>
@scope = https://graph.microsoft.com/.default
@tenantId = <fill-in>
###

# @name tokenResponse
POST https://login.microsoftonline.com/{{tenantId}}/oauth2/v2.0/token HTTP/1.1
Content-Type: application/x-www-form-urlencoded

grant_type={{grantType}}
&client_id={{clientId}}
&client_secret={{clientSecret}}
&scope={{scope}}
```

##### Seal secret and add to Authorization sub system

Seal the secret from the app registration using the [how-to guide](https://github.com/Energinet-DataHub/eo-base-environment/blob/main/docs/guides/how-to-seal-a-secret.md).

- name: `b2c-appregistration-secrets`
- namespace: `eo`
- secret-key name: `ETT_INTERNAL_AUTHORIZATION_SECRETS_SECRET`

Add the sealed secret to the specific Authorization sub system environment in the repository `eo-base-environment`.

##### Add AzureAd data to configmap

Add the following data to the specific Authorization sub system environment configmap (`authorization-cm`), in the repository `eo-base-environment`.

- AzureAd__Instance: https://login.microsoftonline.com/
- AzureAd__ClientId: `<fill-in>`
- AzureAd__TenantId: `<fill-in>`
- AzureAd__ClientCredentials__0__SourceType: ClientSecret

### Deploy custom policies

#### MitID Client Secret

Client secret for MitID integration must be securily stored as a secret in Azure. The secret is referenced by the MitID custom policy deployed later in this guide.

Navigate to `Identity Experience Framework`.

![Identity Experience Framework](images/Custom_policy_secret_identity_experience_framework.png)

Navigate to `Policy keys`.

![Policy keys](images/Custom_policy_keys.png)

Add a new secret value. The value must match the secret configured with the MitID provider. Click `Add`. Choose `Manual`, enter name `B2C_1A_MitIDSecret` and provide the secret value in the Secret input field. Key usage must be set to `Signature`.

![Policy keys](images/Custom_policy_mitid_secret.png)

#### Token Signing Secret

Add secrets used for signing tokens. In the `Policy keys` view, click `Add.`. Choose `Generate`, fill in name `B2C_1A_TokenEncryptionKeyContainer` and choose Key Type `RSA`. Key usage must be set to `Signature`.

![Policy keys](images/Custom_policy_token_signing_key.png)

#### Token Encryption Secret

Perform the same steps as in [link text](#token-signing-secret). Name should be set to `B2C_1A_TokenEncryptionKeyContainer` instead.

![Policy keys](images/Custom_policy_token_encryption_key.png)

#### Policies

The following custom policies must be uploaded to B2C.

- domains/authorization/custom_policies/production/B2C_1A_MitId.XML
- domains/authorization/custom_policies/production/B2C_1A_ClientCredentials.XML

First step is to replace GUIDs and URLs in the custom profiles. The relevant GUIDs and URLs to be replaced are marked with XML comments in the files.

![Policy keys](images/Custom_policy_upload.png)

![Policy keys](images/Custom_policy_upload_mitid.png)

![Policy keys](images/Custom_policy_upload_client_credentials.png)

Notice that it's not possible to test the custom policy from the Azure portal without added a web redirect in the frontnend app registration.

To test the login, add a web application to the ett frontend app with redirect URL `https://jwt.ms`. The login can then be tested, however claims mapping cannot be tested because the app registration does not allow tokens for implicit flow.

![Policy keys](images/Custom_policy_upload_mitid_web_app.png)

Test the custom policy login.

![Policy keys](images/Custom_policy_upload_mitid_web_app_test.png)

#### eo-base configuration

Eo-base configuration needs to be updated. Modify `k8s\energy-origin-apps\authorization\shared\resources\production\authorization-configmap.yaml` with values for
