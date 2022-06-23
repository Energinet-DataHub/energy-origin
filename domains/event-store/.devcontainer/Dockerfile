FROM mcr.microsoft.com/vscode/devcontainers/base:alpine-3.15

COPY --from=golang:1.18-alpine /usr/local/go/ /usr/local/go/

ENV PATH="/usr/local/go/bin:${PATH}"

RUN GOBIN=/usr/local/bin go install sigs.k8s.io/kind@v0.14.0
