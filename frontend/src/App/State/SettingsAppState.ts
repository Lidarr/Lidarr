import AppSectionState, {
  AppSectionDeleteState,
  AppSectionSaveState,
  AppSectionSchemaState,
} from 'App/State/AppSectionState';
import DownloadClient from 'typings/DownloadClient';
import ImportList from 'typings/ImportList';
import Indexer from 'typings/Indexer';
import MetadataProfile from 'typings/MetadataProfile';
import Notification from 'typings/Notification';
import QualityProfile from 'typings/QualityProfile';
import { UiSettings } from 'typings/UiSettings';

export interface DownloadClientAppState
  extends AppSectionState<DownloadClient>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface ImportListAppState
  extends AppSectionState<ImportList>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface IndexerAppState
  extends AppSectionState<Indexer>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface NotificationAppState
  extends AppSectionState<Notification>,
    AppSectionDeleteState {}

export interface QualityProfilesAppState
  extends AppSectionState<QualityProfile>,
    AppSectionSchemaState<QualityProfile> {}

export interface MetadataProfilesAppState
  extends AppSectionState<MetadataProfile>,
    AppSectionSchemaState<MetadataProfile> {}

export type UiSettingsAppState = AppSectionState<UiSettings>;

interface SettingsAppState {
  downloadClients: DownloadClientAppState;
  importLists: ImportListAppState;
  indexers: IndexerAppState;
  metadataProfiles: MetadataProfilesAppState;
  notifications: NotificationAppState;
  qualityProfiles: QualityProfilesAppState;
  uiSettings: UiSettingsAppState;
}

export default SettingsAppState;
