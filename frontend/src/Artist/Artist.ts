import Album from 'Album/Album';
import ModelBase from 'App/ModelBase';

export interface Image {
  coverType: string;
  url: string;
  remoteUrl: string;
}

export interface Statistics {
  albumCount: number;
  trackCount: number;
  trackFileCount: number;
  percentOfTracks: number;
  sizeOnDisk: number;
  totalTrackCount: number;
}

export interface Ratings {
  votes: number;
  value: number;
}

interface Artist extends ModelBase {
  added: string;
  foreignArtistId: string;
  cleanName: string;
  ended: boolean;
  genres: string[];
  images: Image[];
  monitored: boolean;
  overview: string;
  path: string;
  lastAlbum?: Album;
  nextAlbum?: Album;
  qualityProfileId: number;
  metadataProfileId: number;
  monitorNewItems: string;
  ratings: Ratings;
  rootFolderPath: string;
  sortName: string;
  statistics: Statistics;
  status: string;
  tags: number[];
  artistName: string;
  artistType?: string;
  disambiguation?: string;
  isSaving?: boolean;
}

export default Artist;
