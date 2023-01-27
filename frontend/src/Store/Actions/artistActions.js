import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { filterTypePredicates, filterTypes, sortDirections } from 'Helpers/Props';
import { fetchAlbums } from 'Store/Actions/albumActions';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import dateFilterPredicate from 'Utilities/Date/dateFilterPredicate';
import translate from 'Utilities/String/translate';
import { set, updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';

//
// Variables

export const section = 'artist';

export const filters = [
  {
    key: 'all',
    label: () => translate('All'),
    filters: []
  },
  {
    key: 'monitored',
    label: () => translate('MonitoredOnly'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'unmonitored',
    label: () => translate('UnmonitoredOnly'),
    filters: [
      {
        key: 'monitored',
        value: false,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'continuing',
    label: () => translate('ContinuingOnly'),
    filters: [
      {
        key: 'status',
        value: 'continuing',
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'ended',
    label: () => translate('EndedOnly'),
    filters: [
      {
        key: 'status',
        value: 'ended',
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'missing',
    label: () => translate('MissingTracks'),
    filters: [
      {
        key: 'missing',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  }
];

export const filterPredicates = {
  missing: function(item) {
    const { statistics = {} } = item;

    return statistics.trackCount - statistics.trackFileCount > 0;
  },

  nextAlbum: function(item, filterValue, type) {
    return dateFilterPredicate(item.nextAlbum?.releaseDate, filterValue, type);
  },

  lastAlbum: function(item, filterValue, type) {
    return dateFilterPredicate(item.lastAlbum?.releaseDate, filterValue, type);
  },

  added: function(item, filterValue, type) {
    return dateFilterPredicate(item.added, filterValue, type);
  },

  ratings: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.ratings.value * 10, filterValue);
  },

  albumCount: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const albumCount = item.statistics ? item.statistics.albumCount : 0;

    return predicate(albumCount, filterValue);
  },

  sizeOnDisk: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const sizeOnDisk = item.statistics && item.statistics.sizeOnDisk ?
      item.statistics.sizeOnDisk :
      0;

    return predicate(sizeOnDisk, filterValue);
  }
};

export const sortPredicates = {
  status: function(item) {
    let result = 0;

    if (item.monitored) {
      result += 2;
    }

    if (item.status === 'continuing') {
      result++;
    }

    return result;
  },

  sizeOnDisk: function(item) {
    const { statistics = {} } = item;

    return statistics.sizeOnDisk || 0;
  }
};

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  isDeleting: false,
  deleteError: null,
  items: [],
  sortKey: 'sortName',
  sortDirection: sortDirections.ASCENDING,
  pendingChanges: {},
  deleteOptions: {
    addImportListExclusion: false
  }
};

export const persistState = [
  'artist.deleteOptions'
];

//
// Actions Types

export const FETCH_ARTIST = 'artist/fetchArtist';
export const SET_ARTIST_VALUE = 'artist/setArtistValue';
export const SAVE_ARTIST = 'artist/saveArtist';
export const DELETE_ARTIST = 'artist/deleteArtist';

export const TOGGLE_ARTIST_MONITORED = 'artist/toggleArtistMonitored';
export const TOGGLE_ALBUM_MONITORED = 'artist/toggleAlbumMonitored';
export const UPDATE_ARTISTS_MONITOR = 'artist/updateArtistsMonitor';
export const SAVE_ARTIST_EDITOR = 'artist/saveArtistEditor';
export const BULK_DELETE_ARTIST = 'artist/bulkDeleteArtist';

export const SET_DELETE_OPTION = 'artist/setDeleteOption';

//
// Action Creators

export const fetchArtist = createThunk(FETCH_ARTIST);
export const saveArtist = createThunk(SAVE_ARTIST, (payload) => {
  const newPayload = {
    ...payload
  };

  if (payload.moveFiles) {
    newPayload.queryParams = {
      moveFiles: true
    };
  }

  delete newPayload.moveFiles;

  return newPayload;
});

export const deleteArtist = createThunk(DELETE_ARTIST, (payload) => {
  return {
    ...payload,
    queryParams: {
      deleteFiles: payload.deleteFiles,
      addImportListExclusion: payload.addImportListExclusion
    }
  };
});

export const toggleArtistMonitored = createThunk(TOGGLE_ARTIST_MONITORED);
export const toggleAlbumMonitored = createThunk(TOGGLE_ALBUM_MONITORED);
export const updateArtistsMonitor = createThunk(UPDATE_ARTISTS_MONITOR);
export const saveArtistEditor = createThunk(SAVE_ARTIST_EDITOR);
export const bulkDeleteArtist = createThunk(BULK_DELETE_ARTIST);

export const setArtistValue = createAction(SET_ARTIST_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

export const setDeleteOption = createAction(SET_DELETE_OPTION);

//
// Helpers

function getSaveAjaxOptions({ ajaxOptions, payload }) {
  if (payload.moveFolder) {
    ajaxOptions.url = `${ajaxOptions.url}?moveFolder=true`;
  }

  return ajaxOptions;
}

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_ARTIST]: createFetchHandler(section, '/artist'),
  [SAVE_ARTIST]: createSaveProviderHandler(section, '/artist', { getAjaxOptions: getSaveAjaxOptions }),
  [DELETE_ARTIST]: createRemoveItemHandler(section, '/artist'),

  [TOGGLE_ARTIST_MONITORED]: (getState, payload, dispatch) => {
    const {
      artistId: id,
      monitored
    } = payload;

    const artist = _.find(getState().artist.items, { id });

    dispatch(updateItem({
      id,
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/artist/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...artist,
        monitored
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false,
        monitored
      }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false
      }));
    });
  },

  [TOGGLE_ALBUM_MONITORED]: function(getState, payload, dispatch) {
    const {
      artistId: id,
      seasonNumber,
      monitored
    } = payload;

    const artist = _.find(getState().artist.items, { id });
    const seasons = _.cloneDeep(artist.seasons);
    const season = _.find(seasons, { seasonNumber });

    season.isSaving = true;

    dispatch(updateItem({
      id,
      section,
      seasons
    }));

    season.monitored = monitored;

    const promise = createAjaxRequest({
      url: `/artist/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...artist,
        seasons
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      const albums = _.filter(getState().albums.items, { artistId: id, seasonNumber });

      dispatch(batchActions([
        updateItem({
          id,
          section,
          ...data
        }),

        ...albums.map((album) => {
          return updateItem({
            id: album.id,
            section: 'albums',
            monitored
          });
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id,
        section,
        seasons: artist.seasons
      }));
    });
  },

  [UPDATE_ARTISTS_MONITOR]: function(getState, payload, dispatch) {
    const {
      artistIds,
      monitor,
      monitored,
      monitorNewItems,
      shouldFetchAlbumsAfterUpdate = false
    } = payload;

    const artists = [];

    artistIds.forEach((id) => {
      const artistsToUpdate = { id };

      if (monitored != null) {
        artistsToUpdate.monitored = monitored;
      }

      artists.push(artistsToUpdate);
    });

    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/albumStudio',
      method: 'POST',
      data: JSON.stringify({
        artist: artists,
        monitoringOptions: { monitor },
        monitorNewItems
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      if (shouldFetchAlbumsAfterUpdate) {
        dispatch(fetchAlbums({ artistId: artistIds[0] }));
      }

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
  },

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

          const {
            images,
            rootFolderPath,
            statistics,
            ...propsToUpdate
          } = artist;

          return updateItem({
            id: artist.id,
            section: 'artist',
            ...propsToUpdate
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
      // SignaR will take care of removing the artist from the collection

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

  [SET_ARTIST_VALUE]: createSetSettingValueReducer(section),

  [SET_DELETE_OPTION]: (state, { payload }) => {
    return {
      ...state,
      deleteOptions: {
        ...payload
      }
    };
  }

}, defaultState, section);
