import moment from 'moment';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import updateSectionState from 'Utilities/State/updateSectionState';
import naturalExpansion from 'Utilities/String/naturalExpansion';
import { set, update, updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';

//
// Variables

export const section = 'interactiveImport';

const albumsSection = `${section}.albums`;
const trackFilesSection = `${section}.trackFiles`;
let abortCurrentFetchRequest = null;
let abortCurrentRequest = null;
let currentIds = [];

const MAXIMUM_RECENT_FOLDERS = 10;

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  isSaving: false,
  error: null,
  items: [],
  pendingChanges: {},
  sortKey: 'path',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'path',
  secondarySortDirection: sortDirections.ASCENDING,
  recentFolders: [],
  importMode: 'chooseImportMode',
  sortPredicates: {
    path: function(item, direction) {
      const path = item.path;

      return naturalExpansion(path.toLowerCase());
    },

    artist: function(item, direction) {
      const artist = item.artist;

      return artist ? artist.sortName : '';
    },

    quality: function(item, direction) {
      return item.qualityWeight || 0;
    }
  },

  albums: {
    isFetching: false,
    isPopulated: false,
    error: null,
    sortKey: 'albumTitle',
    sortDirection: sortDirections.ASCENDING,
    items: []
  },

  trackFiles: {
    isFetching: false,
    isPopulated: false,
    error: null,
    sortKey: 'relativePath',
    sortDirection: sortDirections.ASCENDING,
    items: []
  }
};

export const persistState = [
  'interactiveImport.sortKey',
  'interactiveImport.sortDirection',
  'interactiveImport.recentFolders',
  'interactiveImport.importMode'
];

//
// Actions Types

export const FETCH_INTERACTIVE_IMPORT_ITEMS = 'interactiveImport/fetchInteractiveImportItems';
export const SAVE_INTERACTIVE_IMPORT_ITEM = 'interactiveImport/saveInteractiveImportItem';
export const SET_INTERACTIVE_IMPORT_SORT = 'interactiveImport/setInteractiveImportSort';
export const UPDATE_INTERACTIVE_IMPORT_ITEM = 'interactiveImport/updateInteractiveImportItem';
export const UPDATE_INTERACTIVE_IMPORT_ITEMS = 'interactiveImport/updateInteractiveImportItems';
export const CLEAR_INTERACTIVE_IMPORT = 'interactiveImport/clearInteractiveImport';
export const ADD_RECENT_FOLDER = 'interactiveImport/addRecentFolder';
export const REMOVE_RECENT_FOLDER = 'interactiveImport/removeRecentFolder';
export const SET_INTERACTIVE_IMPORT_MODE = 'interactiveImport/setInteractiveImportMode';

export const FETCH_INTERACTIVE_IMPORT_ALBUMS = 'interactiveImport/fetchInteractiveImportAlbums';
export const SET_INTERACTIVE_IMPORT_ALBUMS_SORT = 'interactiveImport/clearInteractiveImportAlbumsSort';
export const CLEAR_INTERACTIVE_IMPORT_ALBUMS = 'interactiveImport/clearInteractiveImportAlbums';

export const FETCH_INTERACTIVE_IMPORT_TRACKFILES = 'interactiveImport/fetchInteractiveImportTrackFiles';
export const CLEAR_INTERACTIVE_IMPORT_TRACKFILES = 'interactiveImport/clearInteractiveImportTrackFiles';

//
// Action Creators

export const fetchInteractiveImportItems = createThunk(FETCH_INTERACTIVE_IMPORT_ITEMS);
export const setInteractiveImportSort = createAction(SET_INTERACTIVE_IMPORT_SORT);
export const updateInteractiveImportItem = createAction(UPDATE_INTERACTIVE_IMPORT_ITEM);
export const updateInteractiveImportItems = createAction(UPDATE_INTERACTIVE_IMPORT_ITEMS);
export const saveInteractiveImportItem = createThunk(SAVE_INTERACTIVE_IMPORT_ITEM);
export const clearInteractiveImport = createAction(CLEAR_INTERACTIVE_IMPORT);
export const addRecentFolder = createAction(ADD_RECENT_FOLDER);
export const removeRecentFolder = createAction(REMOVE_RECENT_FOLDER);
export const setInteractiveImportMode = createAction(SET_INTERACTIVE_IMPORT_MODE);

export const fetchInteractiveImportAlbums = createThunk(FETCH_INTERACTIVE_IMPORT_ALBUMS);
export const setInteractiveImportAlbumsSort = createAction(SET_INTERACTIVE_IMPORT_ALBUMS_SORT);
export const clearInteractiveImportAlbums = createAction(CLEAR_INTERACTIVE_IMPORT_ALBUMS);

export const fetchInteractiveImportTrackFiles = createThunk(FETCH_INTERACTIVE_IMPORT_TRACKFILES);
export const clearInteractiveImportTrackFiles = createAction(CLEAR_INTERACTIVE_IMPORT_TRACKFILES);

//
// Action Handlers
export const actionHandlers = handleThunks({
  [FETCH_INTERACTIVE_IMPORT_ITEMS]: function(getState, payload, dispatch) {
    if (abortCurrentFetchRequest) {
      abortCurrentFetchRequest();
      abortCurrentFetchRequest = null;
    }

    if (!payload.downloadId && !payload.folder) {
      dispatch(set({ section, error: { message: '`downloadId` or `folder` is required.' } }));
      return;
    }

    dispatch(set({ section, isFetching: true }));

    const { request, abortRequest } = createAjaxRequest({
      url: '/manualimport',
      data: payload
    });

    abortCurrentFetchRequest = abortRequest;

    request.done((data) => {
      dispatch(batchActions([
        update({ section, data }),

        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null
        })
      ]));
    });

    request.fail((xhr) => {
      if (xhr.aborted) {
        return;
      }

      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr
      }));
    });
  },

  [SAVE_INTERACTIVE_IMPORT_ITEM]: function(getState, payload, dispatch) {
    if (abortCurrentRequest) {
      abortCurrentRequest();
    }

    dispatch(batchActions([
      ...currentIds.map((id) => updateItem({
        section,
        id,
        isReprocessing: false,
        updateOnly: true
      })),
      ...payload.ids.map((id) => updateItem({
        section,
        id,
        isReprocessing: true,
        updateOnly: true
      }))
    ]));

    const items = getState()[section].items;

    const requestPayload = payload.ids.map((id) => {
      const item = items.find((i) => i.id === id);

      return {
        id,
        path: item.path,
        artistId: item.artist ? item.artist.id : undefined,
        albumId: item.album ? item.album.id : undefined,
        albumReleaseId: item.albumReleaseId ? item.albumReleaseId : undefined,
        trackIds: (item.tracks || []).map((e) => e.id),
        isSingleFileRelease: item.isSingleFileRelease,
        cuesheetPath: item.cuesheetPath,
        quality: item.quality,
        releaseGroup: item.releaseGroup,
        downloadId: item.downloadId,
        additionalFile: item.additionalFile,
        replaceExistingFiles: item.replaceExistingFiles,
        disableReleaseSwitching: item.disableReleaseSwitching
      };
    });

    const { request, abortRequest } = createAjaxRequest({
      method: 'POST',
      url: '/manualimport',
      contentType: 'application/json',
      data: JSON.stringify(requestPayload)
    });

    abortCurrentRequest = abortRequest;
    currentIds = payload.ids;

    request.done((data) => {
      dispatch(batchActions(
        data.map((item) => updateItem({
          section,
          ...item,
          isReprocessing: false,
          updateOnly: true
        }))
      ));
    });

    request.fail((xhr) => {
      if (xhr.aborted) {
        return;
      }

      dispatch(batchActions(
        payload.ids.map((id) => updateItem({
          section,
          id,
          isReprocessing: false,
          updateOnly: true
        }))
      ));
    });
  },

  [FETCH_INTERACTIVE_IMPORT_ALBUMS]: createFetchHandler(albumsSection, '/album'),

  [FETCH_INTERACTIVE_IMPORT_TRACKFILES]: createFetchHandler(trackFilesSection, '/trackFile')
});

//
// Reducers

export const reducers = createHandleActions({

  [UPDATE_INTERACTIVE_IMPORT_ITEM]: (state, { payload }) => {
    const id = payload.id;
    const newState = Object.assign({}, state);
    const items = newState.items;
    const index = items.findIndex((item) => item.id === id);
    const item = Object.assign({}, items[index], payload);

    newState.items = [...items];
    newState.items.splice(index, 1, item);

    return newState;
  },

  [UPDATE_INTERACTIVE_IMPORT_ITEMS]: (state, { payload }) => {
    const ids = payload.ids;
    const newState = Object.assign({}, state);
    const items = [...newState.items];

    ids.forEach((id) => {
      const index = items.findIndex((item) => item.id === id);
      const item = Object.assign({}, items[index], payload);

      items.splice(index, 1, item);
    });

    newState.items = items;

    return newState;
  },

  [ADD_RECENT_FOLDER]: function(state, { payload }) {
    const folder = payload.folder;
    const recentFolder = { folder, lastUsed: moment().toISOString() };
    const recentFolders = [...state.recentFolders];
    const index = recentFolders.findIndex((r) => r.folder === folder);

    if (index > -1) {
      recentFolders.splice(index, 1);
    }

    recentFolders.push(recentFolder);

    const sliceIndex = Math.max(recentFolders.length - MAXIMUM_RECENT_FOLDERS, 0);

    return Object.assign({}, state, { recentFolders: recentFolders.slice(sliceIndex) });
  },

  [REMOVE_RECENT_FOLDER]: function(state, { payload }) {
    const folder = payload.folder;
    const recentFolders = [...state.recentFolders];
    const index = recentFolders.findIndex((r) => r.folder === folder);

    recentFolders.splice(index, 1);

    return Object.assign({}, state, { recentFolders });
  },

  [CLEAR_INTERACTIVE_IMPORT]: function(state) {
    const newState = {
      ...defaultState,
      recentFolders: state.recentFolders,
      importMode: state.importMode
    };

    return newState;
  },

  [SET_INTERACTIVE_IMPORT_SORT]: createSetClientSideCollectionSortReducer(section),

  [SET_INTERACTIVE_IMPORT_MODE]: function(state, { payload }) {
    return Object.assign({}, state, { importMode: payload.importMode });
  },

  [SET_INTERACTIVE_IMPORT_ALBUMS_SORT]: createSetClientSideCollectionSortReducer(albumsSection),

  [CLEAR_INTERACTIVE_IMPORT_ALBUMS]: (state) => {
    return updateSectionState(state, albumsSection, {
      ...defaultState.albums
    });
  },

  [CLEAR_INTERACTIVE_IMPORT_TRACKFILES]: (state) => {
    return updateSectionState(state, trackFilesSection, {
      ...defaultState.trackFiles
    });
  }

}, defaultState, section);
