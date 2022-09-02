# Monorepo

* Status: PROPOSAL
* Deciders:
* Date: 2022-09-02

---

## Context and Problem Statement

We need a structured way of creating our tests

---

## Considered Options

* Having it all in one test project
* Split it into multiple test projects to seperate unit and integration test
---

## Decision Outcome

Seperate it into multiple test projects

## Rationale

Microsoft recommend to split it into multiple projects and it seems easier to refactor at a later point if it is seperated.

From microsofts web page:

```
When creating a test project for an app, separate the unit tests from the integration tests into different projects. This helps ensure that infrastructure testing components aren't accidentally included in the unit tests. Separation of unit and integration tests also allows control over which set of tests are run.

Source: https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0
```

