import _ from 'lodash';
import { createAction } from 'redux-actions';
import { update } from 'Store/Actions/baseActions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createFetchSchemaHandler from 'Store/Actions/Creators/createFetchSchemaHandler';
import createRemoveItemHandler from 'Store/Actions/Creators/createRemoveItemHandler';
import createSaveProviderHandler from 'Store/Actions/Creators/createSaveProviderHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';

//
// Variables

const section = 'settings.taggingProfiles';

//
// Actions Types

export const FETCH_TAGGING_PROFILES = 'settings/taggingProfiles/fetchTaggingProfiles';
export const FETCH_TAGGING_PROFILE_SCHEMA = 'settings/taggingProfiles/fetchTaggingProfileSchema';
export const SAVE_TAGGING_PROFILE = 'settings/taggingProfiles/saveTaggingProfile';
export const DELETE_TAGGING_PROFILE = 'settings/taggingProfiles/deleteTaggingProfile';
export const REORDER_TAGGING_PROFILE = 'settings/taggingProfiles/reorderTaggingProfile';
export const SET_TAGGING_PROFILE_VALUE = 'settings/taggingProfiles/setTaggingProfileValue';

//
// Action Creators

export const fetchTaggingProfiles = createThunk(FETCH_TAGGING_PROFILES);
export const fetchTaggingProfileSchema = createThunk(FETCH_TAGGING_PROFILE_SCHEMA);
export const saveTaggingProfile = createThunk(SAVE_TAGGING_PROFILE);
export const deleteTaggingProfile = createThunk(DELETE_TAGGING_PROFILE);
export const reorderTaggingProfile = createThunk(REORDER_TAGGING_PROFILE);

export const setTaggingProfileValue = createAction(SET_TAGGING_PROFILE_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

//
// Details

export default {

  //
  // State

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null,
    isSchemaFetching: false,
    isSchemaPopulated: false,
    schemaError: null,
    schema: {},
    items: [],
    isSaving: false,
    saveError: null,
    pendingChanges: {}
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_TAGGING_PROFILES]: createFetchHandler(section, '/taggingprofile'),
    [FETCH_TAGGING_PROFILE_SCHEMA]: createFetchSchemaHandler(section, '/taggingprofile/schema'),

    [SAVE_TAGGING_PROFILE]: createSaveProviderHandler(section, '/taggingprofile'),
    [DELETE_TAGGING_PROFILE]: createRemoveItemHandler(section, '/taggingprofile'),

    [REORDER_TAGGING_PROFILE]: (getState, payload, dispatch) => {
      const { id, moveIndex } = payload;
      const moveOrder = moveIndex + 1;
      const taggingProfiles = getState().settings.taggingProfiles.items;
      const moving = _.find(taggingProfiles, { id });

      // Don't move if the order hasn't changed
      if (moving.order === moveOrder) {
        return;
      }

      const after = moveIndex > 0 ? _.find(taggingProfiles, { order: moveIndex }) : null;
      const afterQueryParam = after ? `after=${after.id}` : '';

      const promise = createAjaxRequest({
        method: 'PUT',
        url: `/taggingprofile/reorder/${id}?${afterQueryParam}`
      }).request;

      promise.done((data) => {
        dispatch(update({ section, data }));
      });
    }
  },

  //
  // Reducers

  reducers: {
    [SET_TAGGING_PROFILE_VALUE]: createSetSettingValueReducer(section)
  }

};
