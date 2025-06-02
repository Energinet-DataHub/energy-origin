ARG SDK_VERSION
ARG RUNTIME_VERSION

# Base stage with system dependencies
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-noble AS base
WORKDIR /app

# Install Playwright dependencies exactly as in the example
RUN apt-get update && apt-get install -y \
    libnss3 libatk1.0-0 libatk-bridge2.0-0 libcups2 \
    libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 \
    libxrandr2 libgbm1 libasound2 libpangocairo-1.0-0 \
    libx11-xcb1 libxcb1 fonts-noto-cjk \
    libgtk-3-0 libxshmfence1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-noble AS build
ARG PROJECT
WORKDIR /src

# Copy project file first and restore dependencies
COPY ["${PROJECT}", "./"]
RUN dotnet restore "./${PROJECT}"

# Copy the rest of the files
COPY . .

# Modify assembly name as in your original Dockerfile
RUN <<EOR
grep -q "<AssemblyName>" ${PROJECT}
if [ $? -eq 0 ]; then
    sed -i ${PROJECT} -e "s|<AssemblyName>.*</AssemblyName>|<AssemblyName>main</AssemblyName>|"
else
    sed -i ${PROJECT} -e "s|</PropertyGroup>|<AssemblyName>main</AssemblyName></PropertyGroup>|"
fi
EOR

# Build the project
RUN dotnet build "${PROJECT}" -c Release -o /app/build

# Publish stage
FROM build AS publish
ARG PROJECT
RUN dotnet publish "${PROJECT}" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Copy project file to final image as in the example
COPY ["${PROJECT}", "./"]

# Copy utility tools from busybox (from your original Dockerfile)
COPY --from=busybox:uclibc /bin/cp /bin/cp
COPY --from=busybox:uclibc /bin/cat /bin/cat
COPY --from=busybox:uclibc /bin/ls /bin/ls

# Expose ports (from your original Dockerfile)
EXPOSE 8080
EXPOSE 8081

# Set entrypoint to use main assembly name
ENTRYPOINT ["/app/main"]

# Commented out Playwright installation as in the example
# RUN dotnet tool install --global Microsoft.Playwright.CLI
# ENV PATH="${PATH}:/root/.dotnet/tools"
# RUN playwright install --with-deps chromium
