import AlbumAppState from './AlbumAppState';
import ArtistAppState, { ArtistIndexAppState } from './ArtistAppState';
import QueueAppState from './QueueAppState';
import SettingsAppState from './SettingsAppState';
import TagsAppState from './TagsAppState';
import TrackFilesAppState from './TrackFilesAppState';
import TracksAppState from './TracksAppState';

interface FilterBuilderPropOption {
  id: string;
  name: string;
}

export interface FilterBuilderProp<T> {
  name: string;
  label: string;
  type: string;
  valueType?: string;
  optionsSelector?: (items: T[]) => FilterBuilderPropOption[];
}

export interface PropertyFilter {
  key: string;
  value: boolean | string | number | string[] | number[];
  type: string;
}

export interface Filter {
  key: string;
  label: string;
  filers: PropertyFilter[];
}

export interface CustomFilter {
  id: number;
  type: string;
  label: string;
  filers: PropertyFilter[];
}

interface AppState {
  albums: AlbumAppState;
  artist: ArtistAppState;
  artistIndex: ArtistIndexAppState;
  queue: QueueAppState;
  settings: SettingsAppState;
  tags: TagsAppState;
  trackFiles: TrackFilesAppState;
  tracksSelection: TracksAppState;
}

export default AppState;
