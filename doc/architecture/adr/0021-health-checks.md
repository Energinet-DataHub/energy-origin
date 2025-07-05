# Health chekcs

* Status: Accepted
* Deciders: @CodeReaper, @MortMH, @rvplauborg, @Sondergaard, @duizer
* Date: 2023-05-07

---

## Context and Problem Statement

We are building multiple services and we must be able to react if a service is not responding or is experiencing performance issues. Furthermore, changes in performed in the infrastructure (e.g. updating or refactoring configuration) can cause services to break. We must have a simple way of ensuring our changes performed in the infrastructure does not effect a service.

---

## Considered Options

* [Health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-7.0)

A health check is a process that periodically checks the status of a service by sending a request and checking for a response. A service that responds with a status code within a certain range (e.g., 200-299) is considered healthy, while a service that does not respond or responds with an error status code is considered unhealthy.

---

## Decision Outcome

We will use health checks to monitor the availability and health of our services.

We will use a health check endpoint provided by the .NET Core Health Checks middleware. This middleware provides a simple way to define health checks for our services. The endpoint can be accessed via an HTTP GET request to a specified URL. We can define custom health checks to test specific aspects of our services, such as database connectivity, valid configuration, external service dependencies or application-specific features.

We will ensure our health checks covers all non-external dependencies before becoming healthy. Non-external refers to dependencies that are within the control of the domain. For example, a database is within control of the domain, but a service in another domain is not and nor are external systems (like DataHub). 

## Rationale

Implementing health checks is relatively simple in our solution and is supported by our infrastructure.

### Positive Consequences

Using health checks will provide us with a simple and effective way to monitor the availability and health of our services. By defining custom health checks, we can test specific aspects of our services that are critical for their operation.

### Negative Consequences

The main downside of using health checks is the added overhead of periodically sending requests to the services. However, this overhead is usually negligible compared to the benefits of having a monitoring system that can detect and alert us of potential issues. We also need to ensure that our services are properly instrumented to expose the health check endpoint and respond with the correct status codes.
