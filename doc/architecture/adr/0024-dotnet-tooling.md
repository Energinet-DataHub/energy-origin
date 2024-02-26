# Handling dotnet tools

* Status: accepted
* Deciders: @codereaper
* Date: 2024-02-29

---

## Context and Problem Statement

During testing and building both locally and in pipelines there are certain steps that require tooling is available. A tool in this context is anything normally installable by executing a `dotnet tool install ...` command. Typical usage cases for these tools are code generation and maintenance tasks. The pipeline issues observed so far are code generation for gRPC and Entity Framework.

---

## Considered Options

* Use a [tool-manifest](https://learn.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use) file
* Use nugets that includes required tools

---

## Decision Outcome

We chose to use a tool-manifest file.

## Rationale

It was the path of least resistance since the tool-manifest file was already adopted in most subsystems.
