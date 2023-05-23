import { QualityModel } from 'Quality/Quality';
import CustomFormat from './CustomFormat';

export type HistoryEventType =
  | 'grabbed'
  | 'artistFolderImported'
  | 'trackFileImported'
  | 'downloadFailed'
  | 'trackFileDeleted'
  | 'trackFileRenamed'
  | 'albumImportIncomplete'
  | 'downloadImported'
  | 'trackFileRetagged'
  | 'downloadIgnored';

export default interface History {
  episodeId: number;
  seriesId: number;
  sourceTitle: string;
  quality: QualityModel;
  customFormats: CustomFormat[];
  customFormatScore: number;
  qualityCutoffNotMet: boolean;
  date: string;
  downloadId: string;
  eventType: HistoryEventType;
  data: unknown;
  id: number;
}
