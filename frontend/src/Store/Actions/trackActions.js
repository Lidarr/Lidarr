import React from 'react';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import Icon from 'Components/Icon';
import { icons, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import { updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'tracks';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  sortKey: 'mediumNumber',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'absoluteTrackNumber',
  secondarySortDirection: sortDirections.ASCENDING,
  items: [],

  columns: [
    {
      name: 'monitored',
      columnLabel: () => translate('Monitored'),
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'medium',
      label: () => translate('Medium'),
      isVisible: false
    },
    {
      name: 'absoluteTrackNumber',
      label: () => translate('Track'),
      isVisible: true
    },
    {
      name: 'title',
      label: () => translate('Title'),
      isVisible: true
    },
    {
      name: 'path',
      label: () => translate('Path'),
      isVisible: false
    },
    {
      name: 'duration',
      label: () => translate('Duration'),
      isVisible: true
    },
    {
      name: 'audioInfo',
      label: () => translate('AudioInfo'),
      isVisible: true
    },
    {
      name: 'size',
      label: () => translate('Size'),
      isVisible: false
    },
    {
      name: 'customFormats',
      label: 'Formats',
      isVisible: false
    },
    {
      name: 'customFormatScore',
      columnLabel: () => translate('CustomFormatScore'),
      label: React.createElement(Icon, {
        name: icons.SCORE,
        title: () => translate('CustomFormatScore')
      }),
      isVisible: false
    },
    {
      name: 'indexerFlags',
      columnLabel: () => translate('IndexerFlags'),
      label: React.createElement(Icon, {
        name: icons.FLAG,
        title: () => translate('IndexerFlags')
      }),
      isVisible: false
    },
    {
      name: 'status',
      label: () => translate('Status'),
      isVisible: true
    },
    {
      name: 'search',
      columnLabel: () => translate('Search'),
      isVisible: true,
      isModifiable: false
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
  'tracks.sortKey',
  'tracks.sortDirection',
  'tracks.columns'
];

//
// Actions Types

export const FETCH_TRACKS = 'tracks/fetchTracks';
export const SET_TRACKS_SORT = 'tracks/setTracksSort';
export const SET_TRACKS_TABLE_OPTION = 'tracks/setTracksTableOption';
export const CLEAR_TRACKS = 'tracks/clearTracks';
export const TOGGLE_TRACKS_MONITORED = 'tracks/toggleTracksMonitored';

//
// Action Creators

export const fetchTracks = createThunk(FETCH_TRACKS);
export const setTracksSort = createAction(SET_TRACKS_SORT);
export const setTracksTableOption = createAction(SET_TRACKS_TABLE_OPTION);
export const clearTracks = createAction(CLEAR_TRACKS);
export const toggleTracksMonitored = createThunk(TOGGLE_TRACKS_MONITORED);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_TRACKS]: createFetchHandler(section, '/track'),

  [TOGGLE_TRACKS_MONITORED]: function(getState, payload, dispatch) {
    const { trackIds, monitored } = payload;

    dispatch(batchActions(
      trackIds.map((id) => updateItem({
        id,
        section,
        isSaving: true
      }))
    ));

    const promise = createAjaxRequest({
      url: '/track/monitor',
      method: 'PUT',
      data: JSON.stringify({ trackIds, monitored }),
      dataType: 'json'
    }).request;

    promise.done(() => {
      dispatch(batchActions(
        trackIds.map((id) => updateItem({
          id,
          section,
          isSaving: false,
          monitored
        }))
      ));
    });

    promise.fail(() => {
      dispatch(batchActions(
        trackIds.map((id) => updateItem({
          id,
          section,
          isSaving: false
        }))
      ));
    });

    return promise;
  }

});

//
// Reducers

export const reducers = createHandleActions({

  [SET_TRACKS_TABLE_OPTION]: createSetTableOptionReducer(section),

  [FETCH_TRACKS]: (state) => {
    return Object.assign({}, state, {
      isFetching: false,
      isPopulated: false,
      error: null,
      items: []
    });
  },

  [SET_TRACKS_SORT]: createSetClientSideCollectionSortReducer(section)

}, defaultState, section);
