FROM lscr.io/linuxserver/lidarr:latest
COPY src/NzbDrone.Core/bin/Release/*/Lidarr.Core.dll /app/Lidarr.Core.dll
