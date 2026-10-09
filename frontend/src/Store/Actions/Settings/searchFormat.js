import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createSaveHandler from 'Store/Actions/Creators/createSaveHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';

//
// Variables
//

const section = 'settings.searchFormat';

//
// Actions Types
//

export const FETCH_SEARCH_FORMAT_SETTINGS = 'settings/searchFormat/fetchSearchFormatSettings';
export const SAVE_SEARCH_FORMAT_SETTINGS = 'settings/searchFormat/saveSearchFormatSettings';
export const SET_SEARCH_FORMAT_SETTINGS_VALUE = 'settings/searchFormat/setSearchFormatSettingsValue';

//
// Action Creators
//

export const fetchSearchFormatSettings = createThunk(FETCH_SEARCH_FORMAT_SETTINGS);
export const saveSearchFormatSettings = createThunk(SAVE_SEARCH_FORMAT_SETTINGS);
export const setSearchFormatSettingsValue = createAction(SET_SEARCH_FORMAT_SETTINGS_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

//
// Details
//

export default {

  //
  // State
  //

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null,
    pendingChanges: {},
    isSaving: false,
    saveError: null,
    item: {}
  },

  //
  // Action Handlers
  //

  actionHandlers: {
    [FETCH_SEARCH_FORMAT_SETTINGS]: createFetchHandler(section, '/config/searchformat'),
    [SAVE_SEARCH_FORMAT_SETTINGS]: createSaveHandler(section, '/config/searchformat')
  },

  //
  // Reducers
  //

  reducers: {
    [SET_SEARCH_FORMAT_SETTINGS_VALUE]: createSetSettingValueReducer(section)
  }

};
