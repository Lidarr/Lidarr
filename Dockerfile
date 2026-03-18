FROM lscr.io/linuxserver/lidarr:latest
COPY src/NzbDrone.Core/bin/Release/net6.0/NzbDrone.Core.dll /app/NzbDrone.Core.dll
