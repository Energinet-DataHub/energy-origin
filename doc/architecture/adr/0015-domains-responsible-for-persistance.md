# Domains are responsible for their persistance

* Status: Accepted
* Deciders: @MartinSchmidt, @CodeReaper, @duizer, @Exobitt, @endk-awo
* Date: 2022-11-07

---

## Context and Problem Statement

Based on [#0014 Bespoke timeseries domain](0014-timeseries-domain.md) requirement to store
its data within the domain, and removal of all measurements from the shared event-store,
it leaves the question about all other data within the system, where should their data be stored.

Further storing all data centrally exposes internal workings of a single domain to the rest of the system, creating hight coupling between the domains.

---

## Decision Outcome

We chose to move the **responsibility for persisting data to within a domain**, so it is up to each domain (and team) how they chose to store and persist the changes within their domain.

A **curated set of Helm-charts will be made available**, if these are used, then backup and recovery will work "out of the box".
If one chooses **not to use these**, then this **responsiblity falls back to the team**.

The central event store is removed from the system.

## Rationale

Moving the resposibility to each domain empowers each team to choose the technology and solution that best solves the issue at hand.

It also removes a shared central point of failure.
