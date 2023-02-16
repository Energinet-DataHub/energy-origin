# Authentication ORM

* Status: Proposed
* Deciders: @MortMH, @Frydensberg1, @CodeReaper, @robertrosborg
* Date: 2023-02-06

---

## Context and Problem Statement

We need an ORM for the Auth domain to handle entity mapping and database migrations.

---

## Considered Options

* Entity Framework
* Dapper

---

## Decision Outcome

We have chosen Entity Framework.

## Rationale

Both Entity Framework and Dapper are capable ORMs, but we've chosen Entity Framework for the following main reasons:

* Built-in migration handling and generation.
* Improved productivity and faster development.
* Well known and supported in .NET ecosystem.
