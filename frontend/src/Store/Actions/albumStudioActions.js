import { createAction } from 'redux-actions';
import { filterBuilderTypes, filterBuilderValueTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import { fetchAlbums } from './albumActions';
import { filterPredicates, filters } from './artistActions';
import { set } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';

//
// Variables

export const section = 'albumStudio';

//
// State

export const defaultState = {
  isSaving: false,
  saveError: null,
  sortKey: 'sortName',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'sortName',
  secondarySortDirection: sortDirections.ASCENDING,
  selectedFilterKey: 'all',
  filters,
  filterPredicates,

  filterBuilderProps: [
    {
      name: 'monitored',
      label: () => translate('Monitored'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL
    },
    {
      name: 'status',
      label: () => translate('Status'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.ARTIST_STATUS
    },
    {
      name: 'artistType',
      label: () => translate('ArtistType'),
      type: filterBuilderTypes.EXACT
    },
    {
      name: 'qualityProfileId',
      label: () => translate('QualityProfile'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.QUALITY_PROFILE
    },
    {
      name: 'metadataProfileId',
      label: () => translate('MetadataProfile'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.METADATA_PROFILE
    },
    {
      name: 'rootFolderPath',
      label: () => translate('RootFolderPath'),
      type: filterBuilderTypes.EXACT
    },
    {
      name: 'tags',
      label: () => translate('Tags'),
      type: filterBuilderTypes.ARRAY,
      valueType: filterBuilderValueTypes.TAG
    }
  ]
};

export const persistState = [
  'albumStudio.sortKey',
  'albumStudio.sortDirection',
  'albumStudio.selectedFilterKey',
  'albumStudio.customFilters'
];

//
// Actions Types

export const SET_ALBUM_STUDIO_SORT = 'albumStudio/setAlbumStudioSort';
export const SET_ALBUM_STUDIO_FILTER = 'albumStudio/setAlbumStudioFilter';
export const SAVE_ALBUM_STUDIO = 'albumStudio/saveAlbumStudio';

//
// Action Creators

export const setAlbumStudioSort = createAction(SET_ALBUM_STUDIO_SORT);
export const setAlbumStudioFilter = createAction(SET_ALBUM_STUDIO_FILTER);
export const saveAlbumStudio = createThunk(SAVE_ALBUM_STUDIO);

//
// Action Handlers

export const actionHandlers = handleThunks({

  [SAVE_ALBUM_STUDIO]: function(getState, payload, dispatch) {
    const {
      artistIds,
      monitored,
      monitor,
      monitorNewItems
    } = payload;

    const artist = [];

    artistIds.forEach((id) => {
      const artistToUpdate = { id };

      if (payload.hasOwnProperty('monitored')) {
        artistToUpdate.monitored = monitored;
      }

      artist.push(artistToUpdate);
    });

    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/albumStudio',
      method: 'POST',
      data: JSON.stringify({
        artist,
        monitoringOptions: { monitor },
        monitorNewItems
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(fetchAlbums());

      dispatch(set({
        section,
        isSaving: false,
        saveError: null
      }));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_ALBUM_STUDIO_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_ALBUM_STUDIO_FILTER]: createSetClientSideCollectionFilterReducer(section)

}, defaultState, section);

