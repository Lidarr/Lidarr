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

const trackFilesSection = `${section}.trackFiles`;
let abortCurrentFetchRequest = null;
let abortCurrentRequest = null;
let abortCurrentProgressRequest = null;
let progressPollTimer = null;
let currentIds = [];

const MAXIMUM_RECENT_FOLDERS = 10;
const PROGRESS_POLL_INTERVAL = 750;

function createProgressId() {
  return `manual-import-${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

function stopProgressPolling() {
  if (progressPollTimer) {
    window.clearTimeout(progressPollTimer);
    progressPollTimer = null;
  }

  if (abortCurrentProgressRequest) {
    abortCurrentProgressRequest();
    abortCurrentProgressRequest = null;
  }
}

function pollManualImportProgress(dispatch, progressId) {
  if (!progressId) {
    return;
  }

  const { request, abortRequest } = createAjaxRequest({
    url: `/manualimport/progress/${progressId}`
  });

  abortCurrentProgressRequest = abortRequest;

  request.done((progress) => {
    dispatch(set({
      section,
      manualImportProgress: progress
    }));

    if (!progress.isComplete) {
      progressPollTimer = window.setTimeout(() => pollManualImportProgress(dispatch, progressId), PROGRESS_POLL_INTERVAL);
    }
  });

  request.fail((xhr) => {
    if (xhr.aborted) {
      return;
    }

    progressPollTimer = window.setTimeout(() => pollManualImportProgress(dispatch, progressId), PROGRESS_POLL_INTERVAL);
  });
}

function startProgressPolling(dispatch, progressId) {
  stopProgressPolling();

  dispatch(set({
    section,
    manualImportProgress: {
      id: progressId,
      percent: 0,
      message: 'Preparing track identification',
      isComplete: false,
      hasError: false
    }
  }));

  progressPollTimer = window.setTimeout(() => pollManualImportProgress(dispatch, progressId), PROGRESS_POLL_INTERVAL);
}


//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  isSaving: false,
  error: null,
  items: [],
  manualImportProgress: null,
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

    const progressId = createProgressId();
    startProgressPolling(dispatch, progressId);

    dispatch(set({ section, isFetching: true }));

    const { request, abortRequest } = createAjaxRequest({
      url: '/manualimport',
      data: {
        ...payload,
        progressId
      }
    });

    abortCurrentFetchRequest = abortRequest;

    request.done((data) => {
      stopProgressPolling();
      dispatch(batchActions([
        update({ section, data }),

        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null,
          manualImportProgress: {
            id: progressId,
            percent: 100,
            message: 'Manual import scan complete',
            isComplete: true,
            hasError: false
          }
        })
      ]));
    });

    request.fail((xhr) => {
      if (xhr.aborted) {
        return;
      }

      stopProgressPolling();
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr,
        manualImportProgress: {
          id: progressId,
          percent: 0,
          message: 'Manual import scan failed',
          isComplete: true,
          hasError: true
        }
      }));
    });
  },

  [SAVE_INTERACTIVE_IMPORT_ITEM]: function(getState, payload, dispatch) {
    if (abortCurrentRequest) {
      abortCurrentRequest();
    }

    const progressId = createProgressId();
    startProgressPolling(dispatch, progressId);

    dispatch(batchActions([
      set({
        section,
        isSaving: true
      }),
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
        quality: item.quality,
        releaseGroup: item.releaseGroup,
        indexerFlags: item.indexerFlags,
        downloadId: item.downloadId,
        additionalFile: item.additionalFile,
        replaceExistingFiles: item.replaceExistingFiles,
        disableReleaseSwitching: item.disableReleaseSwitching,
        progressId
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
      stopProgressPolling();
      dispatch(batchActions([
        ...data.map((item) => updateItem({
          section,
          ...item,
          isReprocessing: false,
          updateOnly: true
        })),
        set({
          section,
          isSaving: false,
          manualImportProgress: {
            id: progressId,
            percent: 100,
            message: 'Track identification complete',
            isComplete: true,
            hasError: false
          }
        })
      ]));
    });

    request.fail((xhr) => {
      if (xhr.aborted) {
        return;
      }

      stopProgressPolling();
      dispatch(batchActions([
        ...payload.ids.map((id) => updateItem({
          section,
          id,
          isReprocessing: false,
          updateOnly: true
        })),
        set({
          section,
          isSaving: false,
          manualImportProgress: {
            id: progressId,
            percent: 0,
            message: 'Track identification failed',
            isComplete: true,
            hasError: true
          }
        })
      ]));
    });
  },

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
    stopProgressPolling();

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

  [CLEAR_INTERACTIVE_IMPORT_TRACKFILES]: (state) => {
    return updateSectionState(state, trackFilesSection, {
      ...defaultState.trackFiles
    });
  }

}, defaultState, section);
