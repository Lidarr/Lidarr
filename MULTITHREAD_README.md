# Multithreaded library scan / import (this fork)

This branch adds a faster, **parallel** disk scan and import path. Upstream Lidarr does much of this work sequentially; this fork parallelizes folder scanning, tag reads, and release-candidate scoring.

A **Dockerfile.multithread** in this repository builds a self-contained binary and overlays it on `ghcr.io/linuxserver/lidarr:nightly` (see CI or build from repo root per that file’s comments). A wrapper layout that keeps this tree in a `lidarr-src/` subdirectory can use the parent `Dockerfile` instead.

## `LIDARR_MEDIA_IO_PARALLELISM`

Parallel import work is **not** limited by Lidarr’s download bandwidth or rate settings (those apply to indexers/clients only). On **slow or remote storage** (especially NFS), too much concurrency can saturate IOPS and make the host feel stuck. This variable caps how many workers run at once for the fork’s parallel paths.

| | |
| --- | --- |
| **Name** | `LIDARR_MEDIA_IO_PARALLELISM` |
| **Default** | `2` (used when the variable is unset, empty, or not a valid integer) |
| **Allowed** | Integers **1**–**64**; values below **1** fall back to the default |
| **Scope** | Process environment (read once at first use) |

It applies to:

- parallel **folder scans** when collecting audio files;
- parallel **tag / metadata reads** when building import decisions;
- parallel **candidate release scoring** during identification.

**Docker:** fully supported. Set the variable on the container like any other env; the .NET process reads the container environment.

### Docker Compose

```yaml
services:
  lidarr:
    image: your-registry/lidarr-nightly-multithread:latest
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Etc/UTC
      # Gentle on NFS / network mounts; omit for default (2)
      - LIDARR_MEDIA_IO_PARALLELISM=1
```

### `docker run`

```bash
docker run -e LIDARR_MEDIA_IO_PARALLELISM=4 … your-image
```

### When to change it

- **NFS, SMB, or sluggish disks:** try `1` or leave default `2`.
- **Library and app on fast local storage (e.g. same NAS app dataset, local SSD):** try `4`–`8` or higher (up to 64) and watch CPU, I/O, and responsiveness.

### Implementation reference

Logic and constant name: `src/NzbDrone.Common/MediaImportParallelism.cs`.

## Relationship to upstream

Behavior outside scan/import parallelism matches your chosen base (e.g. nightly image + overlaid build). For upstream docs and support channels, see [Lidarr](https://github.com/Lidarr/Lidarr) and the [Servarr wiki](https://wiki.servarr.com/lidarr).
