# Monorepo

* Status: Accepted
* Deciders: @LayZeeDK, @RasmusGodske, @CodeReaper, @Chrisbh, @robertrosborg, @C-Christiansen, @Exobitt, @endk-awo, @PeterAGY, @MartinSchmidt
* Date: 2022-03-14

---

## Context and Problem Statement

Isolating each domain in its own git repository cleanly separates the domains,
but it requires additional tooling and workflows in the repositories to 
automate all of the flows.

---

## Considered Options

* Polyrepo - all domains exist in each their own repository.
* Monorepo - all public domains are stored in one monorepo.

---

## Decision Outcome

The team chose to go with a monorepo setup instead of the existing polyrepo.

## Rationale

A monorepo enables common workflows and code to be easily shared between the domains
without having to use git-submodules.
This lowers the amount of code and work required which should speed up the team.
