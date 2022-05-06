# New Beta Release

Lidarr v1.0.0.2570 has been released on `develop`

- **Users who do not wish to be on the alpha `nightly` testing branch should take advantage of this parity and switch to `develop`**

A reminder about the `develop` and `nightly` branches

- **develop** - Current Develop/Beta - (Beta): This is the testing edge. Released after tested in nightly to ensure no immediate issues. New features and bug fixes released here first after nightly. It can be considered semi-stable, but is still beta.
- **nightly** - Current Nightly/Unstable - (Alpha/Unstable) : This is the bleeding edge. It is released as soon as code is committed and passes all automated tests. This build may have not been used by us or other users yet. There is no guarantee that it will even run in some cases. This branch is only recommended for advanced users. Issues and self investigation are expected in this branch. Use this branch only if you know what you are doing and are willing to get your hands dirty to recover a failed update. This version is updated immediately.

# Announcements

- Automated API Documentation Updates recently implemented
- At long last, a new Lidarr `develop` / beta release
- ** Known Issues **
  - Linux users map experience errors such as:
    - `You have an old version of fpcalc. Please upgrade to 1.4.3.`
    - `An error occurred trying to start process '/opt/Lidarr/fpcalc' with working directory '/'. Permission denied`
  - A fix will be out soon, but this can be fixed by setting fpcalc to be executable `chmod +x /opt/Lidarr/fpcalc`
  - Note that for the fix `sudo` may be required or your path to Lidarr's binary folder may be different depending on your environment and setup.

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

## v1.0.0.2570 (changes since v0.8.1.2135)

 - Fixed: Correct User-Agent api logging

 - Fixed: Default value for MonitorNew

 - New: Option to control which new artist albums get monitored

 - New: Add linux-x86 builds

 - Fixed: UI hiding search results with duplicate GUIDs

 - Fixed: Make sure fpcalc executable after upgrade

 - Fixed: Interactive Search Filter not filtering multiple qualities in the same filter row

 - Fixed: Bundle fpcalc for all builds except FreeBSD

 - New: Add qBittorrent sequential order and first and last piece priority options

 - New: Add backup size information

 - Fixed: IPv4 instead of IP4

 - New: Update Cert Validation Help Text

 - Fixed: Error when trying to import an empty Plex Watchlist

 - Fixed: No longer require first run as admin on windows

 - Fixed: Properly handle 119 error code from Synology Download Station

 - Fixed: Clarify Qbit Content Path Error

 - New: End Jackett 'all' endpoint support

 - New: Add date picker for custom filter dates

 - Fixed: Occasional opus file corruption when writing tags

 - Fixed: Stop downloads requiring manual import from being stuck as Downloaded

 - Fixed: Loading old commands from database

 - Fixed: Error adding album to existing artist in incognito session

 - Fixed: Jumpbar after going back to artist index page

 - Fixed: Cleanup Temp files after backup creation

 - Fixed: Translation warning for search all

 - Fixed: Update from version in logs (#2695)

 - Fixed: Assume SABnzbd develop version is 3.0.0 if not specified

 - Fixed: Improved Indexer test failure message when no results are returned

 - Fixed: Updated ruTorrent stopped state helptext

 - Fixed: Recycle bin log message

 - Fixed: Enable response compression over https

 - Fixed: Handle missing category when getting Qbittorrent download path

 - New: Add AppName to system status response

 - Fixed: Mark as Failed Issues

 - New: OnApplicationUpdate Notifications

 - New: Add Validations for Recycle Bin Folder

 - Fixed: Avoid download path check false positives for flood (#2566)

 - Fixed: Update modal error

 - New: Show previously installed version in Updates UI

 - New: linux-musl-arm builds

 - New: Per download client setting to Remove Completed/Failed

 - Fixed: Queue conflicts with the same download in multiple clients

 - Fixed: Help message when adding download clients

 - Fixed: Download client name in history details

 - Fixed: Sab Removing and DS Various

 - New: Display Unknown Items in Activity Queue by Default

 - New: Support server notifications

 - Fixed: Give a unique name to the cookie

 - Fixed: Forms login persists across restarts in docker

 - Fixed: NullRef in SchemaBuilder when sending payload without optional Provider.Settings fields

 - New: Additional logging for InvalidModel BadRequest API calls

 - Fixed: Windows installer and adding/removing services

 - Fixed: Workaround net6 object serialization issues

 - Fixed: Restarting windows service from UI

 - Fixed: Tray app restart

 - New: Use native .NET socks proxy

 - New: Add osx-arm64 and linux-musl-arm builds

 - New: Build on Net6

 - Fixed: (Translation) .Net Core to .Net

 - Fixed: Add new translate for UI Language

 - Fixed: Manual adding to blocklist

 - Fixed: Write ID3v2 genres as text, not a number

 - Fixed: FileList Search String

 - Fixed: Improve Log Cleansing

 - New: Support reflink on xfs

 - Fixed: Time column is first column on events page

 - Fixed: Prowl notification priority

 - Fixed: Correctly detect mounts in FreeBSD jails

 - Fixed: Calender .ics feed

 - Fixed: Bad login redirect using a reverse proxy

 - New: Add logging is release is rejected because no download URL is available

 - Fixed: Qbit torrents treated as failed after error

 - New: Log which DB is being migrated

 - New: Renamed Blacklist to Blocklist

 - Fixed: Compatibility with the new Download Station API

 - New: Aria2

 - New: Change Today color in calendar for better visibility

 - New: Disable autocomplete of port number

 - New: Localization Framework

 - Fixed: Invalid sortkey on artists.sortName

 - Fixed: Real IP logging when IPv4 is mapped as IPv6

 - Fixed: Log files should not be cached

 - Fixed: Forms login page uses urlbase for logo

 - Fixed: Forms login with urlbase

 - Fixed: UI not updating on upgrade

 - Fixed: Memory leak

 - New: Use native dotnet host and DryIoc

 - New: Use ASP.NET Core instead of Nancy

 - Fixed: Log active indexers instead of implying all indexers are searched

 - Fixed: Updated wiki links for WikiJS

 - Fixed: Corrected Indexer Category Help Text

 - Fixed: Allow repeated import attempts until downloaded files appear

 - Fixed: NullReferenceException manually importing an unparseable release

 - Fixed: Albums added by disk scan have correct monitored status

 - Fixed: Peers filtering in Interactive Search results

 - Fixed: Notifiarr Health Issue Level

 - Fixed: Remove checkbox to unmonitor tracks on delete

 - Fixed: Error changing artist metadata profile

 - New: Add Size column to Activity: Queue (#2310)

 - New: mono disk and process provider cleaning

 - New: Even More Mono Cleaning

 - New: Drop mono support

 - New: .NET 5 support for FreeBSD 11+

 - Fixed: Adding indexers from presets

 - New: Use System.Text.Json for Nancy and SignalR

 - Fixed: Don't ignore default Boolean in db serialization

 - New: Build with NET5

 - Fixed: Parsing RSS with null values

 - New: Mailgun connection

 - New: Webpack 5, UI Package Updates

 - New: Manual Import rejection column is sortable

 - New: Added Prowlarr donation link

 - New: Activity Queue: Rename Timeleft column to Time Left (#2293)

 - New: Indexer Categories no longer Advanced option (#2267)

 - Fixed: Root Folder Downloads check giving errors when RuTorrent is used (#2266)

 - New: Show User Agent in System->Tasks for externally triggered commands  (#2261)

 - New: Health Check for Downloads to Root Folder (#2234)

 - New: Add rel="noreferrer" to all external links

 - Fixed: Refresh queue count when navigating Activity: Queue

 - Fixed: Discordnotifier is now Notifiarr

 - Fixed: Import Lists provider message in UI

 - New: MusicBrainz Series use as import list

 - New: Remove completed downloads from disk when removing from SABnzbd

 - New: ISO 8601 Date format in log files

 - Other bug fixes and improvements, see GitHub history
