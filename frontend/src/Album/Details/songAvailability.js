export function hasSongFile(song) {
  return song.hasFile === true || song.trackFileId > 0;
}
