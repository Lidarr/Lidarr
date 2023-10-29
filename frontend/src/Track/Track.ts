import ModelBase from 'App/ModelBase';

interface Track extends ModelBase {
  artistId: number;
  foreignTrackId: string;
  foreignRecordingId: string;
  trackFileId: number;
  albumId: number;
  explicit: boolean;
  absoluteTrackNumber: number;
  trackNumber: string;
  title: string;
  duration: number;
  trackFile?: object;
  mediumNumber: number;
  hasFile: boolean;
}

export default Track;
