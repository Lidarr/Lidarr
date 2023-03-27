import { createAction } from 'redux-actions';
import { sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import updateSectionState from 'Utilities/State/updateSectionState';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';

//
// Variables

export const section = 'albumSelection';

//
// State

export const defaultState = {
  isFetching: false,
  isReprocessing: false,
  isPopulated: false,
  error: null,
  sortKey: 'releaseDate',
  sortDirection: sortDirections.DESCENDING,
  items: []
};

//
// Actions Types

export const FETCH_ALBUMS = 'albumSelection/fetchAlbums';
export const SET_ALBUMS_SORT = 'albumSelection/setAlbumsSort';
export const CLEAR_ALBUMS = 'albumSelection/clearAlbums';

//
// Action Creators

export const fetchAlbums = createThunk(FETCH_ALBUMS);
export const setAlbumsSort = createAction(SET_ALBUMS_SORT);
export const clearAlbums = createAction(CLEAR_ALBUMS);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_ALBUMS]: createFetchHandler(section, '/album')
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_ALBUMS_SORT]: createSetClientSideCollectionSortReducer(section),

  [CLEAR_ALBUMS]: (state) => {
    return updateSectionState(state, section, {
      ...defaultState
    });
  }

}, defaultState, section);
