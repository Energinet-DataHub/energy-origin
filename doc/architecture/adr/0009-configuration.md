# Configuration in ASP.NET Core

* Status: draft
* Deciders: TBD
* Date: 2022-08-29

---

## Context and Problem Statement

When developing an ASP.NET Core application the application must read its setting from the configuration. When running in production these settings should be read from environment variables. It must be possible to use settings that works only for local development, but does not have any effect on production. Furthermore, writing tests where the code to be tested uses configuration settings should be easy/straightforward.

---

## Considered Options

* [Built-in configuration in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0) plus [Options pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0)
* Static class reading environment variables directly

---

## Decision Outcome

The system will use the **Built-in configuration in ASP.NET Core**

---

## Rationale

The built-in configuration in ASP.NET Core is mature and flexible. It provides a decoupling between the actual source for configuration (e.g. environment variables, files, commandline arguments) and reading a configuration setting.

Adding Options pattern on top of that gives us encapsulation (Classes that depend on configuration settings depend only on the configuration settings that they use) and sepearation of concerns (Settings for different parts of the app aren't dependent or coupled to one another). Also, the options pattern make it simple to mock configuration settings when writing tests.

---

## Guidelines for local development

Settings to be used for local development should be written to appsettings.development.json. This file is not used when running in production.

Remember that appsettings.development.json will be checked in to git, so any secret added to appsettings.development.json will be public.

## Guidelines for options pattern

* Register the Options-classes to the dependency injection service container as `IOptions<TOptions>`. This is done by using `Configure` as shown here: `builder.Services.Configure<PositionOptions>(builder.Configuration.GetRequiredSection(PositionOptions.Position));`
* Prefer small Options-classes grouping related configuration settings
* When more than one Options-class is needed in the application, then use a dedicated configuration section for each Options-class.
