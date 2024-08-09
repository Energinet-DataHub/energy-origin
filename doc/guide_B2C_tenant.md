# Guide for configuring B2C tenant for Energy Origin

## Introduction

### Prerequisites

- Existing tenant
- Access rights to configure tenant

### App registrations

Create registrations for `ett-authorization-b2c` client. This client will be used by custom policies to make requests to `Authorization` service

#### B2C app registration

![App registrations](images/App_registrations.png)

![New app registration](images/App_registrations_new.png)

![New b2c app registration](images/App_registration_b2c.png)

#### Add secret

![New b2c certificate](images/App_registration_b2c_certificate.png)

![New b2c certificate](images/App_registration_b2c_certificate_new.png)

![New b2c certificate](images/App_registration_b2c_certificate_new_props.png)

Set expire to ???

![New b2c certificate](images/App_registration_b2c_certificate_secret_value.png)

Store secret value somewhere safe

Store tenant-id and client-id.

#### Application id

![New b2c certificate](images/App_registration_b2c_application_id.png)

![New b2c certificate](images/App_registration_b2c_application_id_props.png)

Set application id  to `energy-origin`.

![New b2c certificate](images/App_registration_b2c_application_id_get.png)

Get application id url (used in scope when performing a client credentials flow).

#### Test client credentials

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

![New app registration](images/App_registrations_new.png)

![New app registration](images/App_registration_frontend.png)

![New app registration](images/App_registration_frontend_redirect_urls.png)

![New app registration](images/App_registration_frontend_redirect_urls_new.png)

![New app registration](images/App_registration_frontend_redirect_urls_new_props.png)

![New app registration](images/App_registration_frontend_redirect_urls_new_2nd.png)

### Deploy user_info proxy

Go to `API Management services``

![New app registration](images/Api_management_user_info_proxy_new.png)

### Deploy custom policies
