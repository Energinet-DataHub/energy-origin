ARG SDK_VERSION
ARG RUNTIME_VERSION

FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-noble AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-noble AS build
ARG PROJECT
WORKDIR /src/
COPY . .

RUN <<EOR
grep -q "<AssemblyName>" ${PROJECT}
if [ $? -eq 0 ]; then
    sed -i ${PROJECT} -e "s|<AssemblyName>.*</AssemblyName>|<AssemblyName>main</AssemblyName>|"
else
    sed -i ${PROJECT} -e "s|</PropertyGroup>|<AssemblyName>main</AssemblyName></PropertyGroup>|"
fi
EOR

RUN apt-get update && apt-get install -y \
    wget gnupg ca-certificates fonts-liberation \
    libnss3 libatk1.0-0 libatk-bridge2.0-0 libx11-xcb1 libxcomposite1 libxdamage1 \
    libxrandr2 libgbm1 libasound2t64 libpangocairo-1.0-0 libpango-1.0-0 libcairo2 libxshmfence1 \
    --no-install-recommends && rm -rf /var/lib/apt/lists/*

RUN dotnet tool restore || true
RUN dotnet tool install --global Microsoft.Playwright.CLI \
    && /root/.dotnet/tools/playwright install

RUN dotnet publish ${PROJECT} -c Release -o /app/publish
WORKDIR /app/publish
RUN rm -f appsettings.json appsettings.*.json || true

RUN mkdir /playwright && cp -r /root/.cache/ms-playwright /playwright/ms-playwright

FROM base AS final
WORKDIR /app

COPY --from=build /app/publish ./

COPY --from=build /playwright/ms-playwright /root/.cache/ms-playwright

COPY --from=busybox:uclibc /bin/cp /bin/cp
COPY --from=busybox:uclibc /bin/cat /bin/cat
COPY --from=busybox:uclibc /bin/ls /bin/ls

EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["/app/main"]
