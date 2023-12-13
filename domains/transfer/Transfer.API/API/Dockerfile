# Build stage
FROM --platform=$BUILDPLATFORM cgr.dev/chainguard/dotnet-sdk AS build
ARG TARGETARCH
WORKDIR /source

# Copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore -a $TARGETARCH

# Copy and publish app and libraries
COPY . .
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

# Final stage/image
FROM cgr.dev/chainguard/dotnet-runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["./dotnetapp"]
