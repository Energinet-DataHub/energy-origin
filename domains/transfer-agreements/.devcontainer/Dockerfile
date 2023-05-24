FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base

HEALTHCHECK CMD curl --fail http://localhost:8080/health || exit 1
