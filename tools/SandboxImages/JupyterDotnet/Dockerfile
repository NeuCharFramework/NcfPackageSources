ARG JUPYTER_BASE_IMAGE=quay.dockerproxy.net/jupyter/minimal-notebook:latest
FROM ${JUPYTER_BASE_IMAGE}

USER root

ARG DOTNET_CHANNEL=10.0
ARG NUGET_SOURCE=https://api.nuget.org/v3/index.json

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --channel "${DOTNET_CHANNEL}" --install-dir /opt/dotnet \
    && chmod -R a+rX /opt/dotnet \
    && rm -f /tmp/dotnet-install.sh \
    && rm -rf /var/lib/apt/lists/*

ENV DOTNET_ROOT=/opt/dotnet
ENV PATH=/opt/dotnet:/home/jovyan/.dotnet/tools:${PATH}
ENV NUGET_PACKAGES=/opt/ncf/nuget-cache

RUN mkdir -p /opt/ncf/nuget-cache /tmp/ncf-image-build \
    && chmod -R a+rwx /opt/ncf /tmp/ncf-image-build

USER 1000
ENV HOME=/home/jovyan

RUN dotnet --info \
    && dotnet tool install --global Microsoft.dotnet-interactive \
    && dotnet interactive jupyter install

COPY --chown=1000:1000 NuGet.config /tmp/ncf-image-build/NuGet.config
COPY --chown=1000:1000 Learning.csproj /tmp/ncf-image-build/Learning.csproj

RUN dotnet nuget add source "${NUGET_SOURCE}" \
        --name ncf-build-source \
        --configfile /tmp/ncf-image-build/NuGet.config \
    && dotnet restore /tmp/ncf-image-build/Learning.csproj \
        --configfile /tmp/ncf-image-build/NuGet.config \
        --packages "${NUGET_PACKAGES}"

USER root
RUN rm -rf /tmp/ncf-image-build

USER 1000

WORKDIR /home/jovyan/work
