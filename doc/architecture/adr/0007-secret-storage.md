# DevOps - Secret storage

* Status: accepted
* Deciders: @LayZeeDK, @j4k0bk, @RasmusGodske, @lynderup, @MartinSchmidt
* Date: 2021-11-25

---

## Context and Problem Statement

Storing secrets are different than storing the rest of the configuration from
of a system.
Often secrets should be stored in a way that only the target system can use the secrets
after being stored.

---

## Considered Options

* [Azure Key Vault](https://docs.microsoft.com/en-us/azure/aks/csi-secrets-store-driver)
* [Bitnami Sealed-Secrets](https://github.com/bitnami-labs/sealed-secrets)
* [Hashicorp Vault](https://www.vaultproject.io)

---

## Decision Outcome

The system will use **Bitnami Sealed-Secrets**

---

## Rationale

In [ADR-0002 IaC & GitOps](0002-gitops.md) the system defined to use IaC and GitOps.

Sealed-secrets are the only solution of the three which is Cloud Native and
can be configured using GitOps and IaC.

With Sealed-secrets the secret is symmetrically encrypted,
the key is then asymmetrically encrypted and can only be decrypted by the target environment.

The public key used to encrypt the data can be shared with actors that provide secrets,
which will decrease the number of users that raw secrets are exposed to.

### Positive Consequences

* Secrets are versioned together with the rest of the configuration.
* It is Cloud-native.

### Negative Consequences

* No public security audit exists, an external audit will be financed.
