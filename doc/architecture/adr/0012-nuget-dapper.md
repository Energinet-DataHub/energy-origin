# Dapper

* Status: deprecated
* Deciders: @codereaper @martinschmidt
* Date: 2022-08-31

Issue: #729

---

## Deprecation reason

This ADR was relevant when EventStore was proposed as part of ADR-0003. This is no longer relevant. 

## Context and Problem Statement

Taking advantage of a database requires connecting, reading, writing and mapping data. To make this manageable we need a package that databases potentially in the form of an ORM.

---

## Considered Options

* Entity Framework - fully fledged ORM
* Dapper - simple connection with rudimentary mapping

---

## Decision Outcome

We have chosen to use Dapper

## Rationale

Dapper offers full control of what happens when and how while offering data mapping. Dapper can map using `record`s and is easier to create and implement an interface for.

Entity Framework requires a lot of specialty knowledge to avoid pitfalls. EF requires adding the Postgresql license to aproved licenses and has a lot of functionality that is unneeded.

