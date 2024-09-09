# Client Credentials

* Status: Accepted
* Deciders: @sahma19, @TopSwagCode, @martinhenningjensen

* Date: 2024-09-05

---

## Context and Problem Statement

So far we have only had user's interacting with the system through an UI. Now we need to have external 3rd parties acting on behalf of an organization.

---

## Considered Options

### 1. Client Credentials Flow

__Definition__:

This flow is used when the application itself (like a backend service or machine-to-machine API) needs to authenticate with an external service or resource. No end-user is involved in the process.

__Use Case__:

Ideal for server-to-server communication where the service is acting autonomously (not on behalf of any particular user).

__How it Works__:

* The client (e.g., your service) authenticates directly with the authorization server using its own credentials (client ID and secret).
* The server returns an access token, which the client uses to access resources.

__Advantages__:
* Simpler: No user interaction, no need for user login.
* No need to store/refresh long lived user tokens.
* Secure for Server-to-Server: Well-suited for applications processing data autonomously (e.g., processing data for an organization at scheduled intervals).
* Efficiency: No redirects or user involvement, which makes it efficient for backend services.

__Disadvantages__:
* No User Context: Since this flow doesn't involve user authorization, it cannot access resources on behalf of a specific user.

__Best for__:

Autonomous services that perform actions on behalf of the organization (e.g., batch processing, data synchronization).

### 2. Authorization Code Grant Flow
__Definition__: This flow is used when the application needs to act on behalf of a specific user. It requires user authentication and consent to get access to their resources.

__Use Case__:

Ideal for applications where user interaction is necessary, or the application needs to access resources that require user consent.

__How it Works__:

* The client redirects the user to the authorization server, where the user logs in and consents to the requested permissions.
* The authorization server returns an authorization code to the client.
* The client exchanges the authorization code for an access token.

__Advantages__:
* User Context: The service can perform actions on behalf of a specific user.
* Secure for User Data Access: Allows fine-grained control of permissions, specific to user roles.

__Disadvantages__:
* Complexity: Requires user interaction and more steps than Client Credentials Flow.
* User-Dependent: Tokens have shorter lifespans and may need refreshing, which adds complexity.

__Best for__:

Scenarios where user-specific resources need to be accessed or user consent is necessary. Suitable for interactive applications (web, app)


## Decision Outcome

We choose to go with Client Credentials flow, since we are fetching data on an organization level and dont need user interaction as part of the process. We don't need fine grained user level permissions. Simplicity if not needing user logins all the time and managing of user sessions / tokens.
