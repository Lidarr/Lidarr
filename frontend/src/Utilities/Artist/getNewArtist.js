
function getNewArtist(artist, payload) {
  const {
    rootFolderPath,
    monitor,
    monitorNewItems,
    qualityProfileId,
    metadataProfileId,
    artistType,
    tags,
    searchForMissingAlbums = false
  } = payload;

  const addOptions = {
    monitor,
    searchForMissingAlbums
  };

  artist.addOptions = addOptions;
  artist.monitored = true;
  artist.monitorNewItems = monitorNewItems;
  artist.qualityProfileId = qualityProfileId;
  artist.metadataProfileId = metadataProfileId;
  artist.rootFolderPath = rootFolderPath;
  artist.artistType = artistType;
  artist.tags = tags;

  return artist;
}

export default getNewArtist;
