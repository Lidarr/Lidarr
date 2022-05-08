# New Beta Release

Lidarr v1.0.1.2578 has been released on `develop`

- **Users who do not wish to be on the alpha `nightly` testing branch should take advantage of this parity and switch to `develop`**

A reminder about the `develop` and `nightly` branches

- **develop** - Current Develop/Beta - (Beta): This is the testing edge. Released after tested in nightly to ensure no immediate issues. New features and bug fixes released here first after nightly. It can be considered semi-stable, but is still beta.
- **nightly** - Current Nightly/Unstable - (Alpha/Unstable) : This is the bleeding edge. It is released as soon as code is committed and passes all automated tests. This build may have not been used by us or other users yet. There is no guarantee that it will even run in some cases. This branch is only recommended for advanced users. Issues and self investigation are expected in this branch. Use this branch only if you know what you are doing and are willing to get your hands dirty to recover a failed update. This version is updated immediately.

# Announcements

- Automated API Documentation Updates recently implemented

# Additional Commentary

- [Lidarr](https://lidarr.audio/donate), [Prowlarr](https://prowlarr.com/donate), [Radarr](https://radarr.video/donate), [Readarr](https://readarr.com/donate) now accept direct bitcoin donations
- [Readarr official beta on `develop` announced](https://www.reddit.com/r/Readarr/comments/sxvj8y/new_beta_release_develop_v0101248/)
- Radarr Postgres Database Support in `nightly` and `develop`
- Prowlarr Postgres Database Support in `nightly` and `develop`
- [Lidarr Postgres Database Support in development (Draft PR#2625)](https://github.com/Lidarr/Lidarr/pull/2625)
- [Lidarr UDP Syslog Support in  in development (Draft PR#2655)](https://github.com/Lidarr/Lidarr/pull/2655)

# Releases

## Native

- [GitHub Releases](https://github.com/Lidarr/Lidarr/releases)

- [Wiki Installation Instructions](https://wiki.servarr.com/lidarr/installation)

## Docker

- [hotio/Lidarr:testing](https://hotio.dev/containers/lidarr)

- [lscr.io/linuxserver/Lidarr:develop](https://docs.linuxserver.io/images/docker-lidarr)

## NAS Packages

- Synology - Please ask the SynoCommunity to update the base package; however, you can update in-app normally

- QNAP - Please ask the QNAP to update the base package; however, you should be able to update in-app normally

------------

# Release Notes

## v1.0.1.2578 (changes since v1.0.0.2570)

 - Fixed: Custom Script Health Issue Level

 - Fixed: Delay health check notifications on startup

 - Fixed: QBittorrent unknown download state: forcedMetaDL

 - Fixed: Fpcalc not executable after update

 - Other bug fixes and improvements, see GitHub history

## v1.0.0.2570 (changes since v0.8.1.2135)

 - [Review the previous changelog / release post](https://www.reddit.com/r/Lidarr/comments/uk0vl4/new_release_develop_v1002570/)