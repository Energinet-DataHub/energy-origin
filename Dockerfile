ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-jammy-chiseled AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-1-jammy AS build
ARG PROJECT
WORKDIR /src/code/
COPY ${PROJECT} app
COPY ${PROJECT}/../../.config /src/.config
COPY ${PROJECT}/../../Shared /src/Shared
WORKDIR /src/code/app
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
ARG SUBSYSTEM
COPY ${SUBSYSTEM}/migrations /migrations
COPY --from=build /app/publish /app

EXPOSE 80
ENV ASPNETCORE_HTTP_PORTS=80

ENTRYPOINT ["dotnet", "main.dll"]
