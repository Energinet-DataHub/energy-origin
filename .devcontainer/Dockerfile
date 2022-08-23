# FROM mcr.microsoft.com/vscode/devcontainers/base:ubuntu
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:6.0-bullseye
ARG NODE_VERSION="none"
RUN if [ "${NODE_VERSION}" != "none" ]; then su vscode -c "umask 0002 && . /usr/local/share/nvm/nvm.sh && nvm install ${NODE_VERSION} 2>&1"; fi
HEALTHCHECK CMD curl --fail http://localhost:8080/health || exit 1
