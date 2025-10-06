import AppSectionState, {
  AppSectionDeleteState,
  AppSectionItemState,
  AppSectionSaveState,
  AppSectionSchemaState,
} from 'App/State/AppSectionState';
import CustomFormat from 'typings/CustomFormat';
import DownloadClient from 'typings/DownloadClient';
import ImportList from 'typings/ImportList';
import Indexer from 'typings/Indexer';
import IndexerFlag from 'typings/IndexerFlag';
import MetadataProfile from 'typings/MetadataProfile';
import Notification from 'typings/Notification';
import QualityProfile from 'typings/QualityProfile';
import RootFolder from 'typings/RootFolder';
import General from 'typings/Settings/General';
import UiSettings from 'typings/Settings/UiSettings';

export interface DownloadClientAppState
  extends AppSectionState<DownloadClient>,
    AppSectionDeleteState,
    AppSectionSaveState {
  isTestingAll: boolean;
}

export type GeneralAppState = AppSectionItemState<General>;

export interface ImportListAppState
  extends AppSectionState<ImportList>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface IndexerAppState
  extends AppSectionState<Indexer>,
    AppSectionDeleteState,
    AppSectionSaveState {
  isTestingAll: boolean;
}

export interface NotificationAppState
  extends AppSectionState<Notification>,
    AppSectionDeleteState {}

export interface QualityProfilesAppState
  extends AppSectionState<QualityProfile>,
    AppSectionSchemaState<QualityProfile> {}

export interface MetadataProfilesAppState
  extends AppSectionState<MetadataProfile>,
    AppSectionSchemaState<MetadataProfile> {}

export interface CustomFormatAppState
  extends AppSectionState<CustomFormat>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface RootFolderAppState
  extends AppSectionState<RootFolder>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export type IndexerFlagSettingsAppState = AppSectionState<IndexerFlag>;
export type UiSettingsAppState = AppSectionItemState<UiSettings>;

interface SettingsAppState {
  advancedSettings: boolean;
  customFormats: CustomFormatAppState;
  downloadClients: DownloadClientAppState;
  general: GeneralAppState;
  importLists: ImportListAppState;
  indexerFlags: IndexerFlagSettingsAppState;
  indexers: IndexerAppState;
  metadataProfiles: MetadataProfilesAppState;
  notifications: NotificationAppState;
  qualityProfiles: QualityProfilesAppState;
  rootFolders: RootFolderAppState;
  ui: UiSettingsAppState;
}

export default SettingsAppState;
