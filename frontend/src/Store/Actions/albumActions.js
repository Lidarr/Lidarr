import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import albumEntities from 'Album/albumEntities';
import { sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import { updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'albums';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  sortKey: 'releaseDate',
  sortDirection: sortDirections.DESCENDING,
  secondarySortKey: 'title',
  secondarySortDirection: sortDirections.ASCENDING,
  items: [],
  pendingChanges: {},
  sortPredicates: {
    title: ({ title }) => {
      return title.toLocaleLowerCase();
    },
    rating: function(item) {
      return item.ratings.value;
    },
    size: function(item) {
      const { statistics = {} } = item;

      return statistics.sizeOnDisk || 0;
    },
    releaseDate: function({ releaseDate }) {
      return releaseDate || '0';
    }
  },

  columns: [
    {
      name: 'monitored',
      columnLabel: () => translate('Monitored'),
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'title',
      label: () => translate('Title'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'releaseDate',
      label: () => translate('ReleaseDate'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'secondaryTypes',
      label: () => translate('SecondaryTypes'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'mediumCount',
      label: () => translate('MediaCount'),
      isVisible: false
    },
    {
      name: 'trackCount',
      label: () => translate('TrackCount'),
      isVisible: false
    },
    {
      name: 'duration',
      label: () => translate('Duration'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'size',
      label: () => translate('Size'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'rating',
      label: () => translate('Rating'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'status',
      label: () => translate('Status'),
      isVisible: true
    },
    {
      name: 'actions',
      columnLabel: () => translate('Actions'),
      isVisible: true,
      isModifiable: false
    }
  ]
};

export const persistState = [
  'albums.sortKey',
  'albums.sortDirection',
  'albums.columns'
];

//
// Actions Types

export const FETCH_ALBUMS = 'albums/fetchAlbums';
export const SET_ALBUMS_SORT = 'albums/setAlbumsSort';
export const SET_ALBUMS_TABLE_OPTION = 'albums/setAlbumsTableOption';
export const CLEAR_ALBUMS = 'albums/clearAlbums';
export const SET_ALBUM_VALUE = 'albums/setAlbumValue';
export const SAVE_ALBUM = 'albums/saveAlbum';
export const DELETE_ALBUM = 'albums/deleteAlbum';
export const TOGGLE_ALBUM_MONITORED = 'albums/toggleAlbumMonitored';
export const TOGGLE_ALBUMS_MONITORED = 'albums/toggleAlbumsMonitored';

//
// Action Creators

export const fetchAlbums = createThunk(FETCH_ALBUMS);
export const setAlbumsSort = createAction(SET_ALBUMS_SORT);
export const setAlbumsTableOption = createAction(SET_ALBUMS_TABLE_OPTION);
export const clearAlbums = createAction(CLEAR_ALBUMS);
export const toggleAlbumMonitored = createThunk(TOGGLE_ALBUM_MONITORED);
export const toggleAlbumsMonitored = createThunk(TOGGLE_ALBUMS_MONITORED);

export const saveAlbum = createThunk(SAVE_ALBUM);

export const deleteAlbum = createThunk(DELETE_ALBUM, (payload) => {
  return {
    ...payload,
    queryParams: {
      deleteFiles: payload.deleteFiles,
      addImportListExclusion: payload.addImportListExclusion
    }
  };
});

export const setAlbumValue = createAction(SET_ALBUM_VALUE, (payload) => {
  return {
    section: 'albums',
    ...payload
  };
});

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_ALBUMS]: createFetchHandler(section, '/album'),
  [SAVE_ALBUM]: createSaveProviderHandler(section, '/album'),
  [DELETE_ALBUM]: createRemoveItemHandler(section, '/album'),

  [TOGGLE_ALBUM_MONITORED]: function(getState, payload, dispatch) {
    const {
      albumId,
      albumEntity = albumEntities.ALBUMS,
      monitored
    } = payload;

    const albumSection = _.last(albumEntity.split('.'));

    dispatch(updateItem({
      id: albumId,
      section: albumSection,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/album/${albumId}`,
      method: 'PUT',
      data: JSON.stringify({ monitored }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(updateItem({
        id: albumId,
        section: albumSection,
        isSaving: false,
        monitored
      }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id: albumId,
        section: albumSection,
        isSaving: false
      }));
    });
  },

  [TOGGLE_ALBUMS_MONITORED]: function(getState, payload, dispatch) {
    const {
      albumIds,
      albumEntity = albumEntities.ALBUMS,
      monitored
    } = payload;

    dispatch(batchActions(
      albumIds.map((albumId) => {
        return updateItem({
          id: albumId,
          section: albumEntity,
          isSaving: true
        });
      })
    ));

    const promise = createAjaxRequest({
      url: '/album/monitor',
      method: 'PUT',
      data: JSON.stringify({ albumIds, monitored }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(batchActions(
        albumIds.map((albumId) => {
          return updateItem({
            id: albumId,
            section: albumEntity,
            isSaving: false,
            monitored
          });
        })
      ));
    });

    promise.fail((xhr) => {
      dispatch(batchActions(
        albumIds.map((albumId) => {
          return updateItem({
            id: albumId,
            section: albumEntity,
            isSaving: false
          });
        })
      ));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_ALBUMS_SORT]: createSetClientSideCollectionSortReducer(section),

  [SET_ALBUMS_TABLE_OPTION]: createSetTableOptionReducer(section),

  [SET_ALBUM_VALUE]: createSetSettingValueReducer(section),

  [CLEAR_ALBUMS]: (state) => {
    return Object.assign({}, state, {
      isFetching: false,
      isPopulated: false,
      error: null,
      items: []
    });
  }

}, defaultState, section);
