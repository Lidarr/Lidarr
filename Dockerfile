FROM lscr.io/linuxserver/lidarr:latest
COPY src/NzbDrone.Core/bin/Release/net6.0/Lidarr.Core.dll /app/Lidarr.Core.dll
