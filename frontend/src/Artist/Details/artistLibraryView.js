export function getArtistLibraryView(search = '') {
  return new URLSearchParams(search).get('view') === 'songs' ? 'songs' : 'albums';
}

export function getArtistLibraryViewLink(foreignArtistId, view) {
  const path = `/artist/${foreignArtistId}`;
  return view === 'songs' ? `${path}?view=songs` : path;
}
