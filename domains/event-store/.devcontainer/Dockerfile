ARG VARIANT
FROM mcr.microsoft.com/vscode/devcontainers/base:0-${VARIANT}

RUN apt-get update

# golang
ENV GOROOT=/usr/local/go
ENV GOPATH=/go
ENV PATH=${GOPATH}/bin:${GOROOT}/bin:${PATH}
COPY library-scripts/go-debian.sh /tmp/library-scripts/
RUN bash /tmp/library-scripts/go-debian.sh "latest" "${GOROOT}" "${GOPATH}"

# kind
USER vscode
RUN /usr/local/go/bin/go install sigs.k8s.io/kind@v0.14.0
USER root

# dotnet
ENV DOTNET_ROOT="/usr/local/dotnet"
ENV PATH="${PATH}:${DOTNET_ROOT}"
COPY library-scripts/dotnet-debian.sh /tmp/library-scripts/
RUN bash /tmp/library-scripts/dotnet-debian.sh

# kubectl/helm
COPY library-scripts/kubectl-helm-debian.sh /tmp/library-scripts/
RUN bash /tmp/library-scripts/kubectl-helm-debian.sh

# azure cli
COPY library-scripts/azcli-debian.sh /tmp/library-scripts/
RUN bash /tmp/library-scripts/azcli-debian.sh

RUN apt-get clean

# copy-kube-config
COPY copy-kube-config.sh /usr/local/share/
RUN echo "source /usr/local/share/copy-kube-config.sh" | tee -a /root/.bashrc >> /root/.zshrc
USER vscode
RUN echo "source /usr/local/share/copy-kube-config.sh" | tee -a ~/.bashrc >> ~/.zshrc
