ARG VARIANT
FROM mcr.microsoft.com/vscode/devcontainers/base:0-${VARIANT}

ENV DOTNET_ROOT="/usr/local/dotnet"
ENV PATH="${PATH}:${DOTNET_ROOT}"

COPY library-scripts/dotnet-debian.sh /tmp/library-scripts/

RUN apt-get update && bash /tmp/library-scripts/dotnet-debian.sh && apt-get clean
