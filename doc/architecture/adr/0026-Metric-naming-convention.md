# Metric naming convention

* Status: Accepted
* Deciders: @tnickelsen, @ckr123, @sahma19, @TopSwagCode, @martinhenningjensen

* Date: 2024-07-25

---

## Context and Problem Statement

From a system perspective we need to have a common naming convention when creating metrics for our subsystems. Without a guideline we would see a wide range of naming conventions and it would be cumbersome to combine metrics from different subsystems or eventually have naming conflicts.

---

## Decision Outcome

We want to adopt the naming conventions as stated by OpenTelemetry. By using their best practices we expect to have a naming convention that is already tested and conforms to industry standards.

### Naming conventions

We want developers to use the following rules when defining metrics to ensure a consistent use of attribute names.

* Metric name __MUST__ be limited to Basic Latin characters

* Metric name __MUST__ be prefixed with a unique string to avoid clashes with other applications. Names must start with ett_<sub-system>_[metric-name] where sub-system is the same abbreviation as used in infrastructure code. In infrastructure code this is referenced as project_short.

* Metric name __MUST NOT__ be reused. Even if a metric has been discontinued, then the name cannot be used for another purpose later.

* Metric name __SHOULD NOT__ include the unit in the name. Unit should be defined as metadata.
E.g: dh3.edi.process.duration_in_ms is discouraged in favor of dh3.edi.process.duration where the WithUnit metadata has the value ms.
Units may be included when it provides additional meaning to the metric name.

* Metric namespaces __SHOULD NOT__ be pluralized.

* Metric namespace represents a countable concept, then the metric MUST end with count.
E.g: ett.transfer.message.count

### References

* [OpenTelemetry - Attribute naming](https://opentelemetry.io/docs/specs/semconv/general/attribute-naming/)
* [OpenTelemetry - Recommendation for application developers](https://opentelemetry.io/docs/specs/semconv/general/attribute-naming/#recommendations-for-application-developers)
* [OpenTelemetry - Metrics naming](https://opentelemetry.io/docs/specs/semconv/general/metrics/)
