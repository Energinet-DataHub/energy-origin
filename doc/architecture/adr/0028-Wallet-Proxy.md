# Wallet proxy

* Status: Accepted
* Deciders: @tnickelsen, @ckr123, @sahma19, @TopSwagCode, @martinhenningjensen

* Date: 2024-29-08

---

## Context and Problem Statement

Wallet is an opensource project with support for basic token validation with wallet owner as "Sub" claim in that token. Problem is, we now need to support other users and systems need to have access to your wallet. Both systems and users. There for we need a way to secure wallet differently.

---

## Considered Options

Make custom implementation of wallet:
We could fork wallet and maintain our own version of wallet with our authentication and authorization logic.

Pro:
* Fully customizable of logic in wallet.

Con:
* Merge hells, when wallet changes in main repository.
* Might get out of sync with API endpoints and wallets wouldn't work with other wallet systems.

Make auhtorization proxy for wallet:

Make a new subsystem to protect wallet behind as a proxy having custmo logic.

Pro:
* Avoid merge hell.
* Able to customize and add new custom endpoints for our own subsystems without breaking contracts with wallet.
* Wallet internal calls from internal systems would be easy.

Con:
* Need to update proxy endpoints everytime a new wallet version is deployed.
* Create new subsystem we need to support and host.

## Decision Outcome

We went with proxy. Because merging of forks would be a huge burden down the line. So rather take the upfront cost of building a new subsystem that will let us work with the wallet easier in the future without breaking wallet logic.
