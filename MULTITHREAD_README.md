# Multithreaded library scan / import (this fork)

This branch adds a faster, **parallel** disk scan and import path. Upstream Lidarr does much of this work sequentially; this fork parallelizes folder scanning, tag reads, and release-candidate scoring.

A **Dockerfile.multithread** in this repository builds a self-contained binary and overlays it on `ghcr.io/linuxserver/lidarr:nightly` (see CI or build from repo root per that file’s comments). A wrapper layout that keeps this tree in a `lidarr-src/` subdirectory can use the parent `Dockerfile` instead.

## `LIDARR_MEDIA_IO_PARALLELISM` (optional IO cap)

Parallel import work is **not** limited by Lidarr’s download bandwidth or rate settings (those apply to indexers/clients only). On **slow or remote storage** (especially NFS), the default **uncapped** parallelism can saturate IOPS. Set this variable only when you need to **limit** concurrency.

| | |
| --- | --- |
| **Name** | `LIDARR_MEDIA_IO_PARALLELISM` |
| **Omit / empty / invalid / ≤0** | **Original fork behavior:** `Parallel.ForEach` uses the **TPL default** (`MaxDegreeOfParallelism = -1`), which can use **more** concurrent workers than `ProcessorCount` on I/O-heavy work (this is why setting `16` on a 16-core box could feel *slower* than before). **PLINQ** still uses **`ProcessorCount`** (TagLib / candidate scoring cannot use `-1`). |
| **1–64** | Hard cap on **both** `Parallel.ForEach` loops **and** PLINQ degree (same number). Use **`1`–`2`** on NFS if the host stalls. |
| **Scope** | Environment is read when each parallel section runs |

**Docker:** set on the container like any other env variable.

On the first disk scan, Lidarr logs `Media import parallelism:` with **TPL default (-1, uncapped)** or your numeric cap, plus PLINQ degree and host `ProcessorCount`.

### Docker Compose

```yaml
services:
  lidarr:
    image: your-registry/lidarr-nightly-multithread:latest
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Etc/UTC
      # Omit LIDARR_MEDIA_IO_PARALLELISM on fast local storage (max throughput).
      # - LIDARR_MEDIA_IO_PARALLELISM=2   # NFS / slow disk — cap concurrent work
```

### When to set it

- **Fast local RAID / SSD:** **omit** the variable (matches the first multithread fork).
- **NFS or network filesystem:** start with **`2`** (or **`1`**) if scans overwhelm the host.

### Implementation reference

`src/NzbDrone.Common/MediaImportParallelism.cs`.

## Relationship to upstream

Behavior outside scan/import parallelism matches your chosen base (e.g. nightly image + overlaid build). For upstream docs and support channels, see [Lidarr](https://github.com/Lidarr/Lidarr) and the [Servarr wiki](https://wiki.servarr.com/lidarr).
