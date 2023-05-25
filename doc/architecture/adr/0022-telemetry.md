# Telemetry

* Status: Accepted
* Deciders: @MortMH, @CodeReaper, @duizer, @ckr123
* Date: 2023-05-09

---

## Context and Problem Statement

With the increasing complexity of modern distributed systems, it's becoming more and more difficult to trace requests across services, identify performance bottlenecks, and diagnose issues. Traditional monitoring and logging approaches can only provide limited visibility into the system, leaving blind spots and making it hard to pinpoint problems. To address this issue, developers and DevOps teams need a standardized and extensible approach for capturing and analyzing telemetry data across their entire stack.

---

## Considered Options

* Direct integration with telemetry tools (Prometheus/Jaeger)
* OpenTelemetry and OpenTelemetry Collector

---

## Decision Outcome

* OpenTelemetry and OpenTelemetry Collector
    * For now we will focus on metrics/tracing, but OpenTelemetry also sets the stage for logging in the future. Until further notice we will continue to use our current Promtail/Grafana setup for logging.

---

## Rationale

OpenTelemetry provides a vendor-netrual, standardized, open-source framework for collecting, processing, and exporting telemetry data from various sources, including applications, libraries, and infrastructure components. It offers a wide range of integrations with popular telemetry tools and and also a always (including Prometheus and Jaeger that we currently use) and helps future-proof for new technologies by making it easy to switch between different tools without having to change the instrumentation code.
