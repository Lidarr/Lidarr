# Changelog

Local, unreleased changes on `develop` (not yet pushed upstream).

## Unreleased

### Changed
- Album match acceptance threshold loosened from 80% to 75% confidence
  (`CloseAlbumMatchSpecification._albumThreshold` 0.20 → 0.25), so a wider
  range of identification matches are accepted for import.

### Added
- `Dockerfile` and `.dockerignore` for building Lidarr as a container from
  source: multi-stage build (yarn/webpack frontend, .NET 8 SDK backend) into
  a slim `aspnet:8.0` runtime image with `sqlite3` and `libchromaprint-tools`
  (fpcalc) for fingerprint-based track identification. Exposes port 8686
  with `/config` and `/music` volumes. Verified end-to-end on a real Unraid
  host, including fixes for a missing `Logo/` resource copy and disabling
  StyleCop/analyzer enforcement during the Release build.

### Performance
- Disk scan (`DiskScanService.Scan`) now processes files in batches of 500
  instead of one pass over the entire scanned file list, so large libraries
  fill in incrementally in the UI/database during a rescan instead of
  staying empty until every file has been read and matched.
- Fixed an N+1 query in `ImportApprovedTracks`: existing-file lookups during
  a rescan were issued one at a time; now batched up front.

### Fixed
- `DiskScanService.Scan` no longer aborts scanning of all remaining folders
  in a multi-folder batch when one folder has a missing or invalid root
  folder — that folder is now skipped instead.
