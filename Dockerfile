FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Lidarr.sln \
    --configuration Release \
    --output /app \
    --runtime linux-x64 \
    --self-contained false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8686
VOLUME /config
ENTRYPOINT ["dotnet", "Lidarr.dll", "-nobrowser", "-data=/config"]
