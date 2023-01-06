import AppSectionState, {
  AppSectionDeleteState,
  AppSectionSaveState,
} from 'App/State/AppSectionState';
import Artist from 'Artist/Artist';
import Column from 'Components/Table/Column';
import SortDirection from 'Helpers/Props/SortDirection';
import { Filter, FilterBuilderProp } from './AppState';

export interface ArtistIndexAppState {
  sortKey: string;
  sortDirection: SortDirection;
  secondarySortKey: string;
  secondarySortDirection: SortDirection;
  view: string;

  posterOptions: {
    detailedProgressBar: boolean;
    size: string;
    showTitle: boolean;
    showMonitored: boolean;
    showQualityProfile: boolean;
    showNextAlbum: boolean;
    showSearchAction: boolean;
  };

  bannerOptions: {
    detailedProgressBar: boolean;
    size: string;
    showTitle: boolean;
    showMonitored: boolean;
    showQualityProfile: boolean;
    showNextAlbum: boolean;
    showSearchAction: boolean;
  };

  overviewOptions: {
    detailedProgressBar: boolean;
    size: string;
    showMonitored: boolean;
    showQualityProfile: boolean;
    showLastAlbum: boolean;
    showAdded: boolean;
    showAlbumCount: boolean;
    showPath: boolean;
    showSizeOnDisk: boolean;
    showSearchAction: boolean;
  };

  tableOptions: {
    showBanners: boolean;
    showSearchAction: boolean;
  };

  selectedFilterKey: string;
  filterBuilderProps: FilterBuilderProp<Artist>[];
  filters: Filter[];
  columns: Column[];
}

interface ArtistAppState
  extends AppSectionState<Artist>,
    AppSectionDeleteState,
    AppSectionSaveState {
  itemMap: Record<number, number>;

  deleteOptions: {
    addImportListExclusion: boolean;
  };
}

export default ArtistAppState;
