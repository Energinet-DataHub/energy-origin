# Authorization

* Status: Accepted
* Deciders: @tnickelsen, @sahma19 @TopSwagCode @martinhenningjensen @rvplauborg @Sondergaard

* Date: 2024-29-08

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

Below is the actual rating of considered options after :
| Rating type                      | Ory (Self Hosted) | Openiddict (Self Hosted) | IdentityServer (Self Hosted) | Azure Entra ID for Customers / B2C |
| -------------------------------- | ----------------- | ------------------------ | ---------------------------- | ---------------------------------- |
| Development effort               | 2                 | 1                        | 2                            | 3                                  |
| Operations effort                | 1                 | 1                        | 1                            | 3                                  |
| Level of support                 | 1                 | 1                        | 2                            | 3                                  |
| Price tag (incl. support)        | 3                 | 3                        | 1                            | 2                                  |
| GDPR - i.e. do we own the data   | 2                 | 2                        | 2                            | 3                                  |
| Competences required / ecosystem | 2                 | 1                        | 2                            | 3                                  |
| Extensibility/customizability    | 2                 | 3                        | 3                            | 1                                  |
| Supports use cases               | 2                 | 3                        | 3                            | 2                                  |
| Self service                     | 3                 | 1                        | 3                            | 3                                  |
| Security                         | 2                 | 1                        | 2                            | 3                                  |
| Longevity                        | 2                 | 1                        | 2                            | 3                                  |
| Notes                            |                   |                          |                              | In preview unfortunately!          |
| Total                            | 22                | 18                       | 23                           | 29                                 |
## Decision Outcome

We have choosen to go with Azure B2C, because it's a vetted secure hosted solution, that is already used within the company and most likely going to be the standard in the future. We had architect in on this decision and they have scored all the solutions with the team. Azure B2C was the clear winner.
