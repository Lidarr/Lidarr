import { hasSongFile } from 'Album/Details/songAvailability';

// Selection is a draft until saved; filtering must never discard hidden choices.
export function createSongSelection(items, artistSongMode = true) {
  return Object.fromEntries(items.map((item) => [item.groupKey, artistSongMode && item.monitored]));
}

export async function downloadSongSelection({ items, selectedState, search }) {
  const selected = items.filter((item) => selectedState[item.groupKey]);
  if (selected.length) {
    await search(selected.map((item) => item.id));
  }
}

export function filterSongs(items, query, sortKey = 'popularity', libraryFilter = 'all') {
  const search = query.trim().toLocaleLowerCase();
  return items.filter((item) => `${item.title} ${item.albumTitle}`.toLocaleLowerCase().includes(search) &&
      (libraryFilter !== 'downloaded' || hasSongFile(item)) &&
      (libraryFilter !== 'missing' || !hasSongFile(item)))
    .sort((left, right) => {
      const titleOrder = left.title.localeCompare(right.title);
      if (sortKey === 'title') {
        return titleOrder;
      }
      if (sortKey === 'album') {
        return left.albumTitle.localeCompare(right.albumTitle) || titleOrder;
      }
      const leftPosition = left.popularity?.position ?? Number.MAX_SAFE_INTEGER;
      const rightPosition = right.popularity?.position ?? Number.MAX_SAFE_INTEGER;
      return leftPosition - rightPosition || titleOrder;
    });
}

export async function saveSongSelection({ items, selectedState, monitor, enableArtist, search }) {
  const selected = items.filter((item) => selectedState[item.groupKey]);
  const unselected = items.filter((item) => !selectedState[item.groupKey] && item.monitored);
  const unmonitorIds = [...new Set(unselected.flatMap((item) => item.monitorTrackIds))];
  const monitorIds = [...new Set(selected.flatMap((item) => item.monitorTrackIds))];

  // Do not start a search if any part of saving the wanted list fails.
  if (unmonitorIds.length) {
    await monitor(unmonitorIds, false);
  }
  if (monitorIds.length) {
    await monitor(monitorIds, true);
    await enableArtist();
  }
  if (search && selected.length) {
    // One representative per recording, rather than every album appearance.
    await search(selected.map((item) => item.id));
  }
}
