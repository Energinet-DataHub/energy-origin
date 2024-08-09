# Onboarding 3rd party Clients to ETT

This is a small document describing how we want to manually onboard 3rd party clients to Energy Track and Trace. This could be companies like Flexidao or Granular. There are two versions of the onboarding process below. Too Long Didn't Read version and Detailed with screenshots guide

## TLDR Version

Onboarding clients to Demo:

* Goto <https://portal.azure.com/#view/Microsoft_AAD_B2CAdmin/TenantManagementMenuBlade/~/registeredApps> (Remember to be on Developer tenant - datahubeo**u**energinet and not datahubeo**p**energinet)
* Create new client registration with name ett-external-{Name}
* Insert customer into Authorization Database.
  * Run: doc\onboarding-create-client.rest (to make admin call to create a new client)
* Add Client Secret with expire date of default 6 months
  * Note down Client Secret (value field.)
* Test that your newly created client works
  * Run: doc\onboarding-test-client.rest (test that new client can login and that client can make call to our api's)
* Deliver Client Id + Client Secret to customer securely

Same steps for Prod, just with other URL's. At the moment we have issue, that clients onboarded on VClusters also need to be onboarded on Demo, since Azure B2C always ask DEMO if client exists before login.

## Detailed with screenshots guide

First we need to register the new client on azure portal. To do so, we need to go to: <https://portal.azure.com/#view/Microsoft_AAD_B2CAdmin/TenantManagementMenuBlade/~/registeredApps>

press

![New registration](new_registration.png)

to register a new client.

Fill in the form as shown below:

![register client](register-client.png)

We are now redirected to a page where we can see our newly created client.

Client Id can be seen on the following page:

![client id](client_id.png)

On the same page we can click on:

![client credentials link](client_credentials_link.png)

to get into overview of client secrets.

![client secrtes](client_secrets.png)

Here we can click on "New Client Secret", to create a a new client :P secret description, isn't that important and we will stick with default 6 months expire date for now.

![save secret](save_secret.png)

You will get redirected back to prior page with the newly created secret. Copy secret value and store it somewhere secure for now.

With the newly created secret you should be able to test a login with our .REST scripts at:

With Client ID and Client Secret we can now onboard and test to our application:

Creation of client

* doc\onboarding-create-client.rest
  * Use Energinet Issuer Client Credentials to login and get token
  * Use token to Create client.

Test of client

* doc\onboarding-test-client-credentials.rest
  * Login as created client
  * Test client can make API calls. (Get list of consents. Will be empty Result list.)

With everything tested we just need to deliver Client ID and Client Secret to the contact person in a securely manor.
