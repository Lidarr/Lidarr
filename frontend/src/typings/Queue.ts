import ModelBase from 'App/ModelBase';
import { QualityModel } from 'Quality/Quality';
import CustomFormat from 'typings/CustomFormat';

export type QueueTrackedDownloadStatus = 'ok' | 'warning' | 'error';

export type QueueTrackedDownloadState =
  | 'downloading'
  | 'downloadFailed'
  | 'downloadFailedPending'
  | 'importPending'
  | 'importing'
  | 'importFailed'
  | 'imported'
  | 'ignored';

export interface StatusMessage {
  title: string;
  messages: string[];
}

interface Queue extends ModelBase {
  quality: QualityModel;
  customFormats: CustomFormat[];
  size: number;
  title: string;
  sizeleft: number;
  timeleft: string;
  estimatedCompletionTime: string;
  added?: string;
  status: string;
  trackedDownloadStatus: QueueTrackedDownloadStatus;
  trackedDownloadState: QueueTrackedDownloadState;
  statusMessages: StatusMessage[];
  errorMessage: string;
  downloadId: string;
  protocol: string;
  downloadClient: string;
  outputPath: string;
  trackFileCount: number;
  trackHasFileCount: number;
  artistId?: number;
  albumId?: number;
}

export default Queue;
