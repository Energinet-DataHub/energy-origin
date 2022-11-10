# Measurements domain holds data

* Status: Accepted
* Deciders: @MartinSchmidt, @CodeReaper, @duizer, @Exobitt, @endk-awo
* Date: 2022-11-07

---

## Context and Problem Statement

Extension on [#0003](0003-event-store.md) about where to store state,
and how to fill hydrate state in new domains without migration scripts.

The amount of data in the system, ~3.500.000 meterpoints,
each with a measurement for every hour, and to come every 15 minutes,
this would be upwards 153.300.000.000 or 153 billion measurements to hold 5 years data
on an hourly level, and 613 billion with 15 minutes measurements.

It is perceived as infeasible to rehydra query models from events within a reasonable
amount of time.

---

## Considered Options

1. To continue to store all measurements in an central event store, and rehydrate query models in other domains from it.

2. Give measurements domain the responsibility to store and persist meter data. Further it should have APIs to quickly be able to return aggregated or raw data for a number of meters, and expose and posibility for other domains to subscribe to measurements for specific meters and periods.

---

## Decision Outcome

We chose to go with **option 2**.

## Rationale

With measurements stored in a single domain, we can use best in class datestore to store the data, and gain a way to restore from a failure without having to rehydrate the full state from a central eventstore which would take days.
