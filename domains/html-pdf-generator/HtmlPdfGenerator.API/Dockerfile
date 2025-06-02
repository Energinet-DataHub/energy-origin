ARG SDK_VERSION
ARG RUNTIME_VERSION

# Base stage with system dependencies
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-noble AS base
WORKDIR /app

# Install Playwright dependencies
RUN apt-get update && apt-get install -y \
    libnss3 libatk1.0-0 libatk-bridge2.0-0 libcups2 \
    libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 \
    libxrandr2 libgbm1 libasound2 libpangocairo-1.0-0 \
    libx11-xcb1 libxcb1 fonts-noto-cjk \
    libgtk-3-0 libxshmfence1 \
    --no-install-recommends && rm -rf /var/lib/apt/lists/*

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-noble AS build
ARG PROJECT
WORKDIR /src
COPY . .

# Modify assembly name
RUN <<EOR
grep -q "<AssemblyName>" ${PROJECT}
if [ $? -eq 0 ]; then
    sed -i ${PROJECT} -e "s|<AssemblyName>.*</AssemblyName>|<AssemblyName>main</AssemblyName>|"
else
    sed -i ${PROJECT} -e "s|</PropertyGroup>|<AssemblyName>main</AssemblyName></PropertyGroup>|"
fi
EOR

# Restore tools and dependencies
RUN dotnet tool restore || true
RUN dotnet restore "${PROJECT}"

# Build the project
RUN dotnet build "${PROJECT}" -c Release -o /app/build

# Publish stage
FROM build AS publish
ARG PROJECT
RUN dotnet publish "${PROJECT}" -c Release -o /app/publish

# Install Playwright
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN playwright install --with-deps chromium

# Remove unnecessary files
WORKDIR /app/publish
RUN rm -f appsettings.json appsettings.*.json || true

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-noble AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Copy Playwright browsers from publish stage
COPY --from=publish /root/.cache/ms-playwright /root/.cache/ms-playwright

# Copy utility tools from busybox
COPY --from=busybox:uclibc /bin/cp /bin/cp
COPY --from=busybox:uclibc /bin/cat /bin/cat
COPY --from=busybox:uclibc /bin/ls /bin/ls

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set entrypoint
ENTRYPOINT ["/app/main"]
