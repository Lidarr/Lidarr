import ModelBase from 'App/ModelBase';
import Artist from 'Artist/Artist';

export interface Statistics {
  trackCount: number;
  trackFileCount: number;
  percentOfTracks: number;
  sizeOnDisk: number;
  totalTrackCount: number;
}

export interface AlbumRelease {
  id: number;
  albumId: number;
  foreignReleaseId: string;
  title: string;
  status: string;
  duration: number;
  trackCount: number;
  mediumCount: number;
  disambiguation?: string;
  country: string[];
  label: string[];
  format: string;
  monitored: boolean;
}

interface Album extends ModelBase {
  artistId: number;
  artist: Artist;
  foreignAlbumId: string;
  title: string;
  overview: string;
  disambiguation?: string;
  albumType: string;
  monitored: boolean;
  anyReleaseOk: boolean;
  preferredTrackCounts: number[];
  preferredMediumCounts: number[];
  autoBlocklistUnwantedRelease: boolean;
  releases: AlbumRelease[];
  releaseDate: string;
  statistics: Statistics;
  lastSearchTime?: string;
  isSaving?: boolean;
}

export default Album;
