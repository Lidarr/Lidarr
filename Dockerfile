FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build src/NzbDrone.Core/NzbDrone.Core.csproj \
    --configuration Release \
    --framework net6.0

FROM lscr.io/linuxserver/lidarr:latest
COPY --from=build /src/src/NzbDrone.Core/bin/Release/net6.0/NzbDrone.Core.dll \
    /app/NzbDrone.Core.dll
