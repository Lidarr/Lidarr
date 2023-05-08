import Album from 'Album/Album';
import ModelBase from 'App/ModelBase';
import { AppSectionItemState } from 'App/State/AppSectionState';
import Artist from 'Artist/Artist';
import { QualityModel } from 'Quality/Quality';
import CustomFormat from 'typings/CustomFormat';

export interface ArtistTitleInfo {
  title: string;
}

export interface ParsedAlbumInfo {
  albumTitle: string;
  artistName: string;
  artistTitleInfo: ArtistTitleInfo;
  discography: boolean;
  quality: QualityModel;
  releaseGroup?: string;
  releaseHash: string;
  releaseTitle: string;
  releaseTokens: string;
}

export interface ParseModel extends ModelBase {
  title: string;
  parsedAlbumInfo: ParsedAlbumInfo;
  artist?: Artist;
  albums: Album[];
  customFormats?: CustomFormat[];
  customFormatScore?: number;
}

type ParseAppState = AppSectionItemState<ParseModel>;

export default ParseAppState;
