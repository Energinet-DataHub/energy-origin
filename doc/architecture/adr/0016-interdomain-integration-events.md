# Interdomain communication through intergration events.

* Status: Accepted
* Deciders: @MartinSchmidt, @CodeReaper, @duizer, @Exobitt, @endk-awo
* Date: 2022-11-07

---

## Context and Problem Statement

ADR [0015 Domains are responsible for their persistance](0015-domains-responsible-for-persistance.md) removes the central eventstore which creates the new issue how events should populate between domains.

---

## Decision Outcome

We chose to add the concent of integration events, which is events that a domain chooses to expose to the other domains to communicate between domains.

Other domains will be able to subscribe to these events.


## Rationale

Creating a clean seperation on what is domain events (if a domain uses eventsourcing) and integration events, enables individual domains to be rebuilt and redesigned without changes in dependendant domains.

For new domains to be able to replay events enables them to "chatch up" with changes that has happened before their creation, and for other domains to be rebuild if required.
