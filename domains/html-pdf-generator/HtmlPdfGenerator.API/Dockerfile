FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Install Playwright dependencies
RUN apt-get update && apt-get install -y \
    wget gnupg ca-certificates fonts-liberation \
    libnss3 libatk1.0-0 libatk-bridge2.0-0 libx11-xcb1 libxcomposite1 libxdamage1 \
    libxrandr2 libgbm1 libasound2 libpangocairo-1.0-0 libpango-1.0-0 libcairo2 libxshmfence1 \
    --no-install-recommends && rm -rf /var/lib/apt/lists/*

# Install Playwright CLI + download browser
RUN dotnet tool install --global Microsoft.Playwright.CLI \
    && playwright install

ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["dotnet", "PlaywrightPdfService.dll"]
