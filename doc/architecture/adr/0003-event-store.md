# Event store and CQRS

* Status: accepted
* Deciders: @MartinSchmidt
* Date: 2021-07-01

---

## Context and Problem Statement

How to store state in the platform, and how to bring new services up to date
when they are added, without having to create migration scripts between services.

---

## Decision Outcome

The platform will use **Event sourcing** to store data in the form of events in a shared
event store, and each domain will be built using the [**CQRS**](https://martinfowler.com/bliki/CQRS.html) (Command Query Responsibility Segregation) pattern.

---

## Rationale

Storing the events in a centrally shared store removes the criticality in each domain
on how to store and migrate data.

An event in the event store should always be an object in past sense: eg:
UserCreated not CreateUser.

When a new domain is created it can build its state in a query model optimized to
the usecase from the event store.

Each domain can build and optimized datastore or precalculate results needed.
