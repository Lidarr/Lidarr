# syntax=docker/dockerfile:1

# ---- frontend build ----
FROM node:20-bookworm-slim AS frontend
WORKDIR /src
COPY package.json yarn.lock tsconfig.json ./
RUN yarn install --frozen-lockfile
COPY frontend ./frontend
RUN yarn build

# ---- backend build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src
COPY Logo ./Logo
COPY src ./src
# StyleCop/analyzer findings are treated as build errors across most of the
# codebase in Release config; disabled here since this is a plain compile,
# not a lint pass.
ENV NO_ANALYZERS="-p:RunAnalyzersDuringBuild=false -p:EnforceCodeStyleInBuild=false"
RUN dotnet build src/NzbDrone.Console/Lidarr.Console.csproj -c Release $NO_ANALYZERS \
    && dotnet build src/NzbDrone.Mono/Lidarr.Mono.csproj -c Release $NO_ANALYZERS

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends sqlite3 libchromaprint-tools \
    && rm -rf /var/lib/apt/lists/*

COPY --from=backend /src/_output/net8.0 ./
COPY --from=frontend /src/_output/UI ./UI

ENV XDG_CONFIG_HOME=/config \
    LIDARR_DATA=/config

VOLUME /config
VOLUME /music

EXPOSE 8686

ENTRYPOINT ["dotnet", "Lidarr.dll", "-nobrowser", "-data=/config"]
