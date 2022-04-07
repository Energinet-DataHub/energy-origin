# Cloud Native First

* Status: accepted
* Deciders: @MartinSchmidt
* Date: 2021-07-01

---

## Context and Problem Statement

As part of developing the platform, and trying to spark an OpenSource community around it,
it was considered important that the platform should not require a specific cloud platform
to host it.

---

## Decision Outcome

The platform will use [Cloud Native Technology](https://www.cncf.io/about/who-we-are/) as first priority.

---

## Rationale

The platform **must** use **container technology** with **Kubernetes** as orchestrator.

The rationale is that the platform **must** be cloud agnostic, in that it is not locked
to any specific cloud provider.
An OpenSource community would be limited if it only supported a single cloud provider,
and would be locked in with technology dept.

Containers enabled a large degree of freedom, any software that can run on a machine,
can be run in containers.
Further container technologies enables a broad selection of technologies that would
not necessarily be available on a specific cloud.

It more specifically will use the [SCCP platform from Distributed-Technologies](https://github.com/distributed-technologies).

### Positive Consequences

* Can be hosted on any cloud vendor that can supply a K8S cluster or VMs.
* Can be self hosted on own hardware.

### Negative Consequences

* Higher learning curve for developers and operations unknown with the technologies.
