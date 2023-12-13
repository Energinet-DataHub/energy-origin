FROM cgr.dev/chainguard/dotnet:latest-dev@sha256:21c79e93ad81e41a63b8b04ade4f145138febb04dd78da7b0b1ddbfc1356d3f9 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET SDK image to build the application
FROM cgr.dev/chainguard/dotnet-sdk:latest-dev@sha256:eea6f712550c360fe65b0047b786257b2aa40119957e6bea8f018a086c73dcb5 AS build
WORKDIR /src

# Copy the CSPROJ file and restore any dependencies (via NuGet)
COPY ["Transfer.API/API/API.csproj", "Transfer.API/API/"]
RUN dotnet restore "Transfer.API/API/API.csproj"

# Copy the rest of the source code
COPY . .

# Set the working directory to the project directory and build the application
WORKDIR "/src/Transfer.API/API"
RUN dotnet build "API.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
COPY migrations /migrations

ENV ASPNETCORE_HTTP_PORTS=80

ENTRYPOINT ["dotnet", "API.dll"]
