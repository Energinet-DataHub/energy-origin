# Authentication

* Status: accepted
* Deciders: @MartinSchmidt
* Date: 2021-07-01

---

## Context and Problem Statement

How should authentication on the platform happen.

---

## Decision Outcome

**OAuth2 and OIDC** are well established standards and will be used.

## Rationale

OAuth2 is a well established standard for creating tokens for users.

It will be used to create users JWT. It should also enable delegation
on a scope level and device tokens for API access.

OIDC (Open ID Connect) is a standard extending on top of OAuth2 that enables
one to use an external service to authenticate users.
