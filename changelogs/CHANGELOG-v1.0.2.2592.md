# New Stable Release

Lidarr v1.0.2.2592 has been released on `master`

As you know it's been awhile since the last master release, but that is due to the sheer volume of changes - including breaking changes - with v1, so we wanted to ensure it got a good shake out in testing first.

- **Users who do not wish to be on the alpha `nightly` or beta `develop` testing branches should take advantage of this parity and switch to `master`**

A reminder about the `develop` and `nightly` branches

- **develop** - Current Develop/Beta - (Beta): This is the testing edge. Released after tested in nightly to ensure no immediate issues. New features and bug fixes released here first after nightly. It can be considered semi-stable, but is still beta.
- **nightly** - Current Nightly/Unstable - (Alpha/Unstable) : This is the bleeding edge. It is released as soon as code is committed and passes all automated tests. This build may have not been used by us or other users yet. There is no guarantee that it will even run in some cases. This branch is only recommended for advanced users. Issues and self investigation are expected in this branch. Use this branch only if you know what you are doing and are willing to get your hands dirty to recover a failed update. This version is updated immediately.

# Announcements

- Automated API Documentation Updates recently implemented
- At long last, a new Lidarr `master` / stable release
- **Important Notices**
- Lidarr v1 no longer builds for mono and mono support has ceased
- **Lidarr Breaking API Changes**
  - Native ASPCore API Controllers (stricter typing and other small API changes)
- fpcalc is now bundled and no longer a required dependency
- [Jackett `/all` is deprecated and no longer supported. The FAQ has warned about this since May 2021.](https://wiki.servarr.com/radarr/faq#jacketts-all-endpoint)
- Lidarr is now on .Net6
- New builds for OSX Arm64, Linux Musl Arm32, and Linux x86

# Additional Commentary

- [Lidarr](https://lidarr.audio/donate), [Prowlarr](https://prowlarr.com/donate), [Radarr](https://radarr.video/donate), [Readarr](https://readarr.com/donate) now accept direct bitcoin donations
- [Readarr official beta on `develop` announced](https://www.reddit.com/r/Readarr/comments/sxvj8y/new_beta_release_develop_v0101248/)
- Radarr Postgres Database Support in `nightly` and `develop`
- Prowlarr Postgres Database Support in `nightly` and `develop`
- [Lidarr Postgres Database Support in development (Draft PR#2625)](https://github.com/Lidarr/Lidarr/pull/2625)
- Lidarr (and \*Arrs) Wiki Contributions welcomed and strongly encouraged, simply auth with GitHub

# Releases

## Native

- [GitHub Releases](https://github.com/Lidarr/Lidarr/releases)

- [Wiki Installation Instructions](https://wiki.servarr.com/lidarr/installation)

## Docker

- [hotio/Lidarr:release](https://hotio.dev/containers/lidarr)

- [lscr.io/linuxserver/Lidarr:latest](https://docs.linuxserver.io/images/docker-lidarr)

## NAS Packages

- Synology - Please ask the SynoCommunity to update the base package; however, you can update in-app normally

- QNAP - Please ask the QNAP to update the base package; however, you should be able to update in-app normally

------------

# Release Notes

## v1.0.2.2592 (changes since v0.8.1.2135)

- Fixed: (Translation) .Net Core to .Net

- Fixed: Add new translate for UI Language

- Fixed: Adding indexers from presets

- Fixed: Albums added by disk scan have correct monitored status

- Fixed: Allow repeated import attempts until downloaded files appear

- Fixed: Assume SABnzbd develop version is 3.0.0 if not specified

- Fixed: Avoid download path check false positives for flood [(#2566)](https://github.com/lidarr/lidarr/pull/2566)

- Fixed: Bad login redirect using a reverse proxy

- Fixed: Bundle fpcalc for all builds except FreeBSD

- Fixed: Calender .ics feed

- Fixed: Clarify Qbit Content Path Error

- Fixed: Cleanup Temp files after backup creation

- Fixed: Compatibility with the new Download Station API

- Fixed: Correct User-Agent api logging

- Fixed: Corrected Indexer Category Help Text

- Fixed: Correctly detect mounts in FreeBSD jails

- Fixed: Custom Script Health Issue Level

- Fixed: Default value for MonitorNew

- Fixed: Delay health check notifications on startup

- Fixed: Discordnotifier is now Notifiarr

- Fixed: Don't ignore default Boolean in db serialization

- Fixed: Download client name in history details

- Fixed: Enable response compression over https

- Fixed: Error adding album to existing artist in incognito session

- Fixed: Error changing artist metadata profile

- Fixed: Error when trying to import an empty Plex Watchlist

- Fixed: FileList Search String

- Fixed: Forms login page uses urlbase for logo

- Fixed: Forms login persists across restarts in docker

- Fixed: Forms login with urlbase

- Fixed: Fpcalc not executable after update

- Fixed: Give a unique name to the cookie

- Fixed: Handle missing category when getting Qbittorrent download path

- Fixed: Help message when adding download clients

- Fixed: Import Lists provider message in UI

- Fixed: Improve Log Cleansing

- Fixed: Improved Indexer test failure message when no results are returned

- Fixed: Interactive Search Filter not filtering multiple qualities in the same filter row

- Fixed: Invalid sortkey on artists.sortName

- Fixed: IPv4 instead of IP4

- Fixed: Jumpbar after going back to artist index page

- Fixed: Loading old commands from database

- Fixed: Log active indexers instead of implying all indexers are searched

- Fixed: Log files should not be cached

- Fixed: Make sure fpcalc executable after upgrade

- Fixed: Manual adding to blocklist

- Fixed: Mark as Failed Issues

- Fixed: Memory leak

- Fixed: No longer require first run as admin on windows

- Fixed: Notifiarr Health Issue Level

- Fixed: NullRef in SchemaBuilder when sending payload without optional Provider.Settings fields

- Fixed: NullReferenceException manually importing an unparseable release

- Fixed: Occasional opus file corruption when writing tags

- Fixed: Parsing RSS with null values

- Fixed: Peers filtering in Interactive Search results

- Fixed: Properly handle 119 error code from Synology Download Station

- Fixed: Prowl notification priority

- Fixed: Qbit torrents treated as failed after error

- Fixed: QBittorrent unknown download state: forcedMetaDL

- Fixed: Queue conflicts with the same download in multiple clients

- Fixed: Real IP logging when IPv4 is mapped as IPv6

- Fixed: Recycle bin log message

- Fixed: Refresh queue count when navigating Activity: Queue

- Fixed: Remove checkbox to unmonitor tracks on delete

- Fixed: Restarting windows service from UI

- Fixed: Root Folder Downloads check giving errors when RuTorrent is used [(#2266)](https://github.com/lidarr/lidarr/pull/2266)

- Fixed: Sab Removing and DS Various

- Fixed: Stop downloads requiring manual import from being stuck as Downloaded

- Fixed: Time column is first column on events page

- Fixed: Translation warning for search all

- Fixed: Tray app restart

- Fixed: UI hiding search results with duplicate GUIDs

- Fixed: UI not updating on upgrade

- Fixed: Update from version in logs [(#2695)](https://github.com/lidarr/lidarr/pull/2695)

- Fixed: Update modal error

- Fixed: Updated ruTorrent stopped state helptext

- Fixed: Updated wiki links for WikiJS

- Fixed: Windows installer and adding/removing services

- Fixed: Workaround net6 object serialization issues

- Fixed: Write ID3v2 genres as text, not a number

- New: .NET 5 support for FreeBSD 11+

- New: Activity Queue: Rename Timeleft column to Time Left [(#2293)](https://github.com/lidarr/lidarr/pull/2293)

- New: Add AppName to system status response

- New: Add backup size information

- New: Add date picker for custom filter dates

- New: Add linux-x86 builds

- New: Add logging is release is rejected because no download URL is available

- New: Add osx-arm64 and linux-musl-arm builds

- New: Add qBittorrent sequential order and first and last piece priority options

- New: Add rel="noreferrer" to all external links

- New: Add Size column to Activity: Queue [(#2310)](<https://github.com/lidarr/lidarr/pull/2310>

- New: Add Validations for Recycle Bin Folder

- New: Added Prowlarr donation link

- New: Added UDP syslog support

- New: Additional logging for InvalidModel BadRequest API calls

- New: Aria2

- New: Build on Net6

- New: Build with NET5

- New: Change Today color in calendar for better visibility

- New: Disable autocomplete of port number

- New: Display Unknown Items in Activity Queue by Default

- New: Drop mono support

- New: End Jackett 'all' endpoint support

- New: Even More Mono Cleaning

- New: Health Check for Downloads to Root Folder [(#2234)](https://github.com/lidarr/lidarr/pull/2234)

- New: Indexer Categories no longer Advanced option [(#2267)](https://github.com/lidarr/lidarr/pull/2267)

- New: Instance name for Page Title

- New: Instance name in System/Status API endpoint

- New: Instance Name used for Syslog

- New: ISO 8601 Date format in log files

- New: linux-musl-arm builds

- New: Localization Framework

- New: Log which DB is being migrated

- New: Mailgun connection

- New: Manual Import rejection column is sortable

- New: mono disk and process provider cleaning

- New: MusicBrainz Series use as import list

- New: OnApplicationUpdate Notifications

- New: Option to control which new artist albums get monitored

- New: Per download client setting to Remove Completed/Failed

- New: Remove completed downloads from disk when removing from SABnzbd

- New: Renamed Blacklist to Blocklist

- New: Set Instance Name

- New: Show previously installed version in Updates UI

- New: Show User Agent in System->Tasks for externally triggered commands  [(#2261)](https://github.com/lidarr/lidarr/pull/2261)

- New: Support reflink on xfs

- New: Support server notifications

- New: Update Cert Validation Help Text

- New: Use ASP.NET Core instead of Nancy

- New: Use native .NET socks proxy

- New: Use native dotnet host and DryIoc

- New: Use System.Text.Json for Nancy and SignalR

- New: Webpack 5, UI Package Updates

- Other bug fixes and improvements, see GitHub history
