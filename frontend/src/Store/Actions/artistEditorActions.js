import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { filterBuilderTypes, filterBuilderValueTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import { filterPredicates, filters, sortPredicates } from './artistActions';
import { set, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'artistEditor';

//
// State

export const defaultState = {
  isSaving: false,
  saveError: null,
  isDeleting: false,
  deleteError: null,
  sortKey: 'sortName',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'sortName',
  secondarySortDirection: sortDirections.ASCENDING,
  selectedFilterKey: 'all',
  filters,
  filterPredicates,

  columns: [
    {
      name: 'status',
      columnLabel: translate('Status'),
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'sortName',
      label: translate('Name'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'qualityProfileId',
      label: translate('QualityProfile'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'metadataProfileId',
      label: translate('MetadataProfile'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'path',
      label: translate('Path'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'sizeOnDisk',
      label: translate('SizeOnDisk'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'tags',
      label: translate('Tags'),
      isSortable: true,
      isVisible: true
    }
  ],

  filterBuilderProps: [
    {
      name: 'monitored',
      label: translate('Monitored'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL
    },
    {
      name: 'status',
      label: translate('Status'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.ARTIST_STATUS
    },
    {
      name: 'qualityProfileId',
      label: translate('QualityProfile'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.QUALITY_PROFILE
    },
    {
      name: 'metadataProfileId',
      label: translate('MetadataProfile'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.METADATA_PROFILE
    },
    {
      name: 'path',
      label: translate('Path'),
      type: filterBuilderTypes.STRING
    },
    {
      name: 'rootFolderPath',
      label: translate('RootFolderPath'),
      type: filterBuilderTypes.EXACT
    },
    {
      name: 'sizeOnDisk',
      label: translate('SizeOnDisk'),
      type: filterBuilderTypes.NUMBER,
      valueType: filterBuilderValueTypes.BYTES
    },
    {
      name: 'tags',
      label: translate('Tags'),
      type: filterBuilderTypes.ARRAY,
      valueType: filterBuilderValueTypes.TAG
    }
  ],

  sortPredicates
};

export const persistState = [
  'artistEditor.sortKey',
  'artistEditor.sortDirection',
  'artistEditor.selectedFilterKey',
  'artistEditor.customFilters',
  'artistEditor.columns'
];

//
// Actions Types

export const SET_ARTIST_EDITOR_SORT = 'artistEditor/setArtistEditorSort';
export const SET_ARTIST_EDITOR_FILTER = 'artistEditor/setArtistEditorFilter';
export const SAVE_ARTIST_EDITOR = 'artistEditor/saveArtistEditor';
export const BULK_DELETE_ARTIST = 'artistEditor/bulkDeleteArtist';
export const SET_ARTIST_EDITOR_TABLE_OPTION = 'artistEditor/setArtistEditorTableOption';

//
// Action Creators

export const setArtistEditorSort = createAction(SET_ARTIST_EDITOR_SORT);
export const setArtistEditorFilter = createAction(SET_ARTIST_EDITOR_FILTER);
export const saveArtistEditor = createThunk(SAVE_ARTIST_EDITOR);
export const bulkDeleteArtist = createThunk(BULK_DELETE_ARTIST);
export const setArtistEditorTableOption = createAction(SET_ARTIST_EDITOR_TABLE_OPTION);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [SAVE_ARTIST_EDITOR]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/artist/editor',
      method: 'PUT',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        ...data.map((artist) => {
          return updateItem({
            id: artist.id,
            section: 'artist',
            ...artist
          });
        }),

        set({
          section,
          isSaving: false,
          saveError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  },

  [BULK_DELETE_ARTIST]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isDeleting: true
    }));

    const promise = createAjaxRequest({
      url: '/artist/editor',
      method: 'DELETE',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done(() => {
      // SignalR will take care of removing the artist from the collection

      dispatch(set({
        section,
        isDeleting: false,
        deleteError: null
      }));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isDeleting: false,
        deleteError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_ARTIST_EDITOR_TABLE_OPTION]: createSetTableOptionReducer(section),
  [SET_ARTIST_EDITOR_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_ARTIST_EDITOR_FILTER]: createSetClientSideCollectionFilterReducer(section)

}, defaultState, section);
