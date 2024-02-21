import ModelBase from 'App/ModelBase';
import { QualityModel } from 'Quality/Quality';
import CustomFormat from 'typings/CustomFormat';
import MediaInfo from 'typings/MediaInfo';

export interface TrackFile extends ModelBase {
  artistId: number;
  albumId: number;
  path: string;
  size: number;
  dateAdded: string;
  sceneName: string;
  releaseGroup: string;
  quality: QualityModel;
  customFormats: CustomFormat[];
  indexerFlags: number;
  mediaInfo: MediaInfo;
  qualityCutoffNotMet: boolean;
}
