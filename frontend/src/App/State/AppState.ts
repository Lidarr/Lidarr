import ParseAppState from 'App/State/ParseAppState';
import AlbumAppState from './AlbumAppState';
import ArtistAppState, { ArtistIndexAppState } from './ArtistAppState';
import CalendarAppState from './CalendarAppState';
import CommandAppState from './CommandAppState';
import HistoryAppState from './HistoryAppState';
import QueueAppState from './QueueAppState';
import RootFolderAppState from './RootFolderAppState';
import SettingsAppState from './SettingsAppState';
import SystemAppState from './SystemAppState';
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

export interface AppSectionState {
  version: string;
  dimensions: {
    isSmallScreen: boolean;
    width: number;
    height: number;
  };
}

interface AppState {
  albums: AlbumAppState;
  app: AppSectionState;
  artist: ArtistAppState;
  artistIndex: ArtistIndexAppState;
  calendar: CalendarAppState;
  commands: CommandAppState;
  history: HistoryAppState;
  parse: ParseAppState;
  queue: QueueAppState;
  rootFolders: RootFolderAppState;
  settings: SettingsAppState;
  tags: TagsAppState;
  trackFiles: TrackFilesAppState;
  tracksSelection: TracksAppState;
  system: SystemAppState;
}

export default AppState;
