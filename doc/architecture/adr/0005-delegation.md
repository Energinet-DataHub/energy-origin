# Internal delegation

* Status: accepted
* Deciders: @MartinSchmidt
* Date: 2021-07-01

---

## Context and Problem Statement

How should the platform handle delegation of access to other users data.

---

## Decision Outcome

Delegation events when access is granted, updated or revoked
should be created and added to the event store.

This ensures that the domains are not dependant on a single service to be able to
respond to requests.

This also ensures there is a clear history of when, to what and by whom delegation was created
or revoked.
