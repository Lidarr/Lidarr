import moment from 'moment';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withCurrentPage from 'Components/withCurrentPage';
import { searchMissing, setCalendarDaysCount, setCalendarFilter } from 'Store/Actions/calendarActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { createCustomFiltersSelector } from 'Store/Selectors/createClientSideCollectionSelector';
import createArtistCountSelector from 'Store/Selectors/createArtistCountSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import { isCommandExecuting } from 'Utilities/Command';
import isBefore from 'Utilities/Date/isBefore';
import CalendarPage from './CalendarPage';

function createMissingAlbumIdsSelector() {
  return createSelector(
    (state) => state.calendar.start,
    (state) => state.calendar.end,
    (state) => state.calendar.items,
    (state) => state.queue.details.items,
    (start, end, albums, queueDetails) => {
      return albums.reduce((acc, album) => {
        const releaseDate = album.releaseDate;

        if (
          album.percentOfTracks < 100 &&
          moment(releaseDate).isAfter(start) &&
          moment(releaseDate).isBefore(end) &&
          isBefore(album.releaseDate) &&
          !queueDetails.some((details) => !!details.album && details.album.id === album.id)
        ) {
          acc.push(album.id);
        }

        return acc;
      }, []);
    }
  );
}

function createIsSearchingSelector() {
  return createSelector(
    (state) => state.calendar.searchMissingCommandId,
    createCommandsSelector(),
    (searchMissingCommandId, commands) => {
      if (searchMissingCommandId == null) {
        return false;
      }

      return isCommandExecuting(commands.find((command) => {
        return command.id === searchMissingCommandId;
      }));
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    (state) => state.calendar.selectedFilterKey,
    (state) => state.calendar.filters,
    createCustomFiltersSelector('calendar'),
    createArtistCountSelector(),
    createUISettingsSelector(),
    createMissingAlbumIdsSelector(),
    createCommandExecutingSelector(commandNames.RSS_SYNC),
    createIsSearchingSelector(),
    (
      selectedFilterKey,
      filters,
      customFilters,
      artistCount,
      uiSettings,
      missingAlbumIds,
      isRssSyncExecuting,
      isSearchingForMissing
    ) => {
      return {
        selectedFilterKey,
        filters,
        customFilters,
        colorImpairedMode: uiSettings.enableColorImpairedMode,
        hasArtist: !!artistCount.count,
        artistError: artistCount.error,
        artistIsFetching: artistCount.isFetching,
        artistIsPopulated: artistCount.isPopulated,
        missingAlbumIds,
        isRssSyncExecuting,
        isSearchingForMissing
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onRssSyncPress() {
      dispatch(executeCommand({
        name: commandNames.RSS_SYNC
      }));
    },

    onSearchMissingPress(albumIds) {
      dispatch(searchMissing({ albumIds }));
    },

    onDaysCountChange(dayCount) {
      dispatch(setCalendarDaysCount({ dayCount }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setCalendarFilter({ selectedFilterKey }));
    }
  };
}

export default withCurrentPage(
  connect(createMapStateToProps, createMapDispatchToProps)(CalendarPage)
);
