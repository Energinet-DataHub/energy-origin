# Authentication ORM

* Status: Proposed
* Deciders: @MortMH, @Frydensberg1, @CodeReaper, @robertrosborg
* Date: 2023-02-06

---

## Context and Problem Statement

MartenDB EventStore has previously been proposed as the database of choice, but [ADR-0015](0015-domains-responsible-for-persistance.md) allows each domain to choose the database that best suits it. For the Auth domain we've therefore decided to go for a simpler solution in the form of a relational PostgreSQL database.

Because of this we need an ORM to handle entity mapping and database migrations.

---

## Considered Options

* Entity Framework
* Dapper

---

## Decision Outcome

We have chosen Entity Framework.

## Rationale

Both Entity Framework and Dapper are capable ORMs and a choice between them will often come down to personal preferences, but the main differentiating reasons we've chosen Entity Framework are:

* Built-in migration handling and generation.
* Improved productivity and faster development.
* Well known and supported in .NET ecosystem.
