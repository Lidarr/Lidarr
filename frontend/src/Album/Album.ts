import ModelBase from 'App/ModelBase';

export interface Statistics {
  trackCount: number;
  trackFileCount: number;
  percentOfTracks: number;
  sizeOnDisk: number;
  totalTrackCount: number;
}

interface Album extends ModelBase {
  foreignAlbumId: string;
  title: string;
  overview: string;
  disambiguation?: string;
  monitored: boolean;
  releaseDate: string;
  statistics: Statistics;
}

export default Album;
