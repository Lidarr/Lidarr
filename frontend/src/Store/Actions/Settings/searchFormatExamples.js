import { batchActions } from 'redux-batched-actions';
import { set, update } from 'Store/Actions/baseActions';
import { createThunk } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';

//
// Variables
//

const section = 'settings.searchFormatExamples';

//
// Actions Types
//

export const FETCH_SEARCH_FORMAT_EXAMPLES = 'settings/searchFormatExamples/fetchSearchFormatExamples';

//
// Action Creators
//

export const fetchSearchFormatExamples = createThunk(FETCH_SEARCH_FORMAT_EXAMPLES);

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
    item: {}
  },

  //
  // Action Handlers
  //

  actionHandlers: {
    [FETCH_SEARCH_FORMAT_EXAMPLES]: function(getState, payload, dispatch) {
      dispatch(set({ section, isFetching: true }));

      const searchFormat = getState().settings.searchFormat;

      const promise = createAjaxRequest({
        url: '/config/searchformat/examples',
        data: Object.assign({}, searchFormat.item, searchFormat.pendingChanges)
      }).request;

      promise.done((data) => {
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

      promise.fail((xhr) => {
        dispatch(set({
          section,
          isFetching: false,
          isPopulated: false,
          error: xhr
        }));
      });
    }
  },

  //
  // Reducers
  //

  reducers: {}

};
