# GETTING STARTED WITH LIDARR

## Introduction
Lidarr is a powerful music collection manager designed for Usenet and BitTorrent users. This guide is aimed to get you up and running with Lidarr in no time.

## Features
- **Platform Support:** Windows, Linux, macOS, Raspberry Pi, and more.
- **Automatic Track Detection:** Lidarr keeps an eye out for new tracks from your favorite artists.
- **Library Management:** Scan your existing library and get missing tracks.
- **Quality Upgrades:** Get better quality tracks automatically.
- **Manual Search:** Pick releases manually and understand why some weren't auto-downloaded.
- **Renaming:** Full control over track renaming.
- **Integration:** Full support for SABnzbd, NZBGet, Kodi, and Plex.
- **Specials:** Manage special tracks and multi-album releases.
- **UI:** A user-friendly and beautiful interface.

## Installation

For a quick installation guide based on your operating system, see below:

### Setting Up Reverse Proxy

#### NGINX Configuration:

1. Add the provided configuration to `nginx.conf` located at the root of your Nginx configuration. Make sure to include it inside the server context.
   
```nginx
location ^~ /lidarr {
    proxy_pass http://127.0.0.1:8686;
    ...
}
# Allow the API External Access via NGINX
location ~ /lidarr/api {
    ...
}
```
Refer to the [wiki](https://wiki.servarr.com/lidarr) for detailed Nginx configurations, including subdomain setups.

#### Apache Configuration:

1. If you wish to use the root of a domain or subdomain, adjust the Location block accordingly.

```apache
<Location /lidarr>
  ProxyPreserveHost on
  ...
</Location>
```

For a dedicated VirtualHost for Lidarr:

```apache
ProxyPass / http://127.0.0.1:8686/lidarr/
...
```
Check the [wiki](https://wiki.servarr.com/lidarr) for more details about Apache configurations.

For other platforms, Docker deployments, or more in-depth installation guidelines, please refer to the Lidarr [wiki](https://wiki.servarr.com/lidarr).

## Initial Configuration

After installation, Lidarr will need to be configured based on your requirements. Referring to the [Lidarr wiki](https://wiki.servarr.com/lidarr) will help guide through RSS setup, library management, and integration with other platforms.

## Advanced Features (For Developers)

### Development Tools and Environment:

- **Languages:** Backend in C# and Frontend in JS.
- **Frameworks:** Backend on .NET6 and Frontend on Reactjs.
- **Tools Required:**
  - [Visual Studio 2022](https://www.visualstudio.com/vs/) (Community version is free).
  - An HTML/Javascript editor of your choice (VS Code, Sublime Text, Webstorm, Atom, etc.).
  - Git.
  - Node.js runtime (Versions 12.0, 14.0, 16.0, or later).
  - Yarn for building the frontend.

### Getting Started with Development:

1. **Frontend:**
   - Navigate to the cloned directory.
   - Install required Node Packages using `yarn install`.
   - Start webpack for monitoring with `yarn start`.

2. **Backend:**
   - Set startup project in Visual Studio to Lidarr.Console with net6.0 framework.
   - Build the solution first, then Debug/Run the project.
   - Access Lidarr at `http://localhost:8686`.

For more detailed setup, building, and contribution guidelines, please refer to the "How to Contribute" section in the Lidarr wiki.

## Troubleshooting and Support

For bugs and feature requests, it's best to utilize [GitHub Issues](https://github.com/Lidarr/Lidarr/issues). Note that only actual bugs and feature requests should be posted there. 

For general questions, discussions, or immediate assistance, the following channels are available:
- [Discord Chat](https://lidarr.audio/discord)
- [Reddit Discussion](https://www.reddit.com/r/lidarr)

Additionally, the [Lidarr Wiki](https://wiki.servarr.com/lidarr) is a rich source of information and can answer many commonly asked questions.