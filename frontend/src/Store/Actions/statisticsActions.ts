import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import { filterBuilderTypes, filterBuilderValueTypes } from 'Helpers/Props';
import { StatisticsParams } from 'Statistics/getStatisticsParams';
import { createThunk, handleThunks } from 'Store/thunks';
import RootFolder from 'typings/RootFolder';
import Statistics from 'typings/Statistics';
import sortByProp from 'Utilities/Array/sortByProp';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import { set, update } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';

//
// Variables

export const section = 'statistics';

let abortCurrentRequest: (() => void) | null = null;

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  item: {},

  selectedFilterKey: 'all',

  filters: [
    {
      key: 'all',
      label: () => translate('All'),
      filters: [],
    },
  ],

  // Names map to API parameters in Statistics/getStatisticsParams
  filterBuilderProps: [
    {
      name: 'rootFolderPath',
      label: () => translate('RootFolder'),
      type: filterBuilderTypes.EXACT,
      optionsSelector: (rootFolders: RootFolder[]) => {
        return rootFolders
          .map((rootFolder) => ({ id: rootFolder.path, name: rootFolder.path }))
          .sort(sortByProp('name'));
      },
    },
    {
      name: 'monitored',
      label: () => translate('Monitored'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL,
    },
    {
      name: 'qualityProfileId',
      label: () => translate('QualityProfile'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.QUALITY_PROFILE,
    },
    {
      name: 'metadataProfileId',
      label: () => translate('MetadataProfile'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.METADATA_PROFILE,
    },
    {
      name: 'tags',
      label: () => translate('Tags'),
      type: filterBuilderTypes.ARRAY,
      valueType: filterBuilderValueTypes.TAG,
    },
  ],
};

export const persistState = ['statistics.selectedFilterKey'];

//
// Actions Types

export const FETCH_STATISTICS = 'statistics/fetchStatistics';
export const SET_STATISTICS_FILTER = 'statistics/setStatisticsFilter';

//
// Action Creators

export const fetchStatistics = createThunk(FETCH_STATISTICS);
export const setStatisticsFilter = createAction(SET_STATISTICS_FILTER);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_STATISTICS]: function (
    _getState: () => AppState,
    payload: StatisticsParams,
    dispatch: Dispatch
  ) {
    if (abortCurrentRequest) {
      abortCurrentRequest();
    }

    dispatch(set({ section, isFetching: true }));

    const { request, abortRequest } = createAjaxRequest({
      url: '/statistics',
      data: payload,
      traditional: true,
    });

    abortCurrentRequest = abortRequest;

    // An older filter's request can still settle after this one starts: abort can't stop a completed one
    const isLatest = () => abortCurrentRequest === abortRequest;

    request.done((data: Statistics) => {
      if (!isLatest()) {
        return;
      }

      dispatch(
        batchActions([
          update({ section, data }),

          set({
            section,
            isFetching: false,
            isPopulated: true,
            error: null,
          }),
        ])
      );
    });

    request.fail((xhr: unknown) => {
      if (!isLatest()) {
        return;
      }

      dispatch(
        set({
          section,
          isFetching: false,
          isPopulated: false,
          error: xhr,
        })
      );
    });
  },
});

//
// Reducers

export const reducers = createHandleActions(
  {
    [SET_STATISTICS_FILTER]:
      createSetClientSideCollectionFilterReducer(section),
  },
  defaultState,
  section
);
