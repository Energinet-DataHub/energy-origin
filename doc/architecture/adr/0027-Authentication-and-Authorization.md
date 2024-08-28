# Metric naming convention

* Status: Accepted
* Deciders: @tnickelsen, @ckr123, @sahma19, @TopSwagCode, @martinhenningjensen

* Date: 2025-01-01

---

## Context and Problem Statement

We need a way to handle authentication and authorization of these use cases:

* A mitid user needs to be able to securely view and do actions on their data.
* A mitid needs to be able to give 3rd party system access to work on their data.
* A user needs to be able to give 3rd party mitid user access to work on their data.

---

## Considered Options

Auth0 / other cloud / hosted 3rd party solutions. Unknown / long Vetting proccess. Would block the project for long and may never be vetted for use at Energinet.

Hydra / Keyclock / other self hosted solutions. Handling the security aspects of running an identity provider. Having java / golang knownledge to help with customization of these products. Handling backup of data.

Azure B2C. Other teams has used this before. Needing to learning custom policies xml to make customizations. Already vetted products that we can start using today.

## Decision Outcome

We have choosen to go with Azure B2C, because it's a vetted secure hosted solution, that is already used within the company and most likely going to be the standard in the future. We had acritect in on this decision and they have scored all the solutions with the team. Azure B2C was the clear winner.
