import Quality from 'Quality/Quality';

export interface QualityProfileStatistics {
  qualityProfileId: number;
  name: string;
  artistCount: number;
  trackFileCount: number;
  sizeOnDisk: number;
}

export interface MetadataProfileStatistics {
  metadataProfileId: number;
  name: string;
  artistCount: number;
}

export interface QualityStatistics {
  quality: Quality;
  trackFileCount: number;
  sizeOnDisk: number;
}

export interface TagStatistics {
  tagId: number;
  label: string;
  artistCount: number;
  trackFileCount: number;
  sizeOnDisk: number;
}

export interface AlbumTypeStatistics {
  albumType: string;
  albumCount: number;
}

export interface SecondaryTypeStatistics {
  secondaryType: string;
  albumCount: number;
}

export interface DecadeStatistics {
  decade: number | null;
  albumCount: number;
  trackFileCount: number;
  sizeOnDisk: number;
}

export interface GenreStatistics {
  genre: string;
  albumCount: number;
  trackFileCount: number;
  sizeOnDisk: number;
}

export interface LargestArtistStatistics {
  artistId: number;
  artistName: string;
  trackFileCount: number;
  sizeOnDisk: number;
}

export interface MediaFormatStatistics {
  // Left out of the JSON for albums of unknown format
  mediaFormat?: string;
  albumCount: number;
}

interface Statistics {
  artistCount: number;
  monitoredArtistCount: number;
  completedArtistCount: number;
  continuingArtistCount: number;
  endedArtistCount: number;
  deletedArtistCount: number;
  albumCount: number;
  monitoredAlbumCount: number;
  completedAlbumCount: number;
  missingAlbumCount: number;
  totalTrackCount: number;
  monitoredTrackCount: number;
  downloadedTrackCount: number;
  missingTrackCount: number;
  unreleasedTrackCount: number;
  trackFileCount: number;
  sizeOnDisk: number;
  qualityProfileStatistics: QualityProfileStatistics[];
  metadataProfileStatistics: MetadataProfileStatistics[];
  qualityStatistics: QualityStatistics[];
  tagStatistics: TagStatistics[];
  albumTypeStatistics: AlbumTypeStatistics[];
  secondaryTypeStatistics: SecondaryTypeStatistics[];
  decadeStatistics: DecadeStatistics[];
  genreStatistics: GenreStatistics[];
  largestArtistStatistics: LargestArtistStatistics[];
  mediaFormatStatistics: MediaFormatStatistics[];
}

export default Statistics;
