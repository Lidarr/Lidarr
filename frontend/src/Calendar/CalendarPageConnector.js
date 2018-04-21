import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setCalendarDaysCount, setCalendarFilter } from 'Store/Actions/calendarActions';
import createArtistCountSelector from 'Store/Selectors/createArtistCountSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import CalendarPage from './CalendarPage';

function createMapStateToProps() {
  return createSelector(
    (state) => state.calendar,
    createArtistCountSelector(),
    createUISettingsSelector(),
    (calendar, artistCount, uiSettings) => {
      return {
        selectedFilterKey: calendar.selectedFilterKey,
        filters: calendar.filters,
        showUpcoming: calendar.showUpcoming,
        colorImpairedMode: uiSettings.enableColorImpairedMode,
        hasArtist: !!artistCount
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onDaysCountChange(dayCount) {
      dispatch(setCalendarDaysCount({ dayCount }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setCalendarFilter({ selectedFilterKey }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(CalendarPage);
