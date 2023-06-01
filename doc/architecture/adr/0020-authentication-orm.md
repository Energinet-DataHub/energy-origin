# Authentication ORM

* Status: Accepted
* Deciders: @MortMH, @Frydensberg1, @CodeReaper, @robertrosborg
* Date: 2023-02-06

---

## Context and Problem Statement

MartenDB EventStore has previously been proposed as the database of choice, but [ADR-0015](0015-domains-responsible-for-persistance.md) allows each domain to choose the database that best suits it. For the Auth domain we have therefore decided to go for a simpler solution in the form of a relational database. PostgreSQL has previously been the go-to choice and we found no reason to change this.

We looked into finding an ORM to handle entity mapping and database migrations.

---

## Considered Options

* Entity Framework
* Dapper

---

## Decision Outcome

We have chosen Entity Framework.

## Rationale

Both Entity Framework and Dapper are capable ORMs and a choice between them will often come down to personal preferences, but the main differentiating reasons we have chosen Entity Framework are:

* Built-in migration handling and generation.
* Avoidance of manually writing SQL.
* Well known and supported in .NET ecosystem.
* Improved productivity and faster development.
