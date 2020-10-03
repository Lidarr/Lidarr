import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllArtistSelector from 'Store/Selectors/createAllArtistSelector';
import TagDetailsModalContent from './TagDetailsModalContent';

function findMatchingItems(ids, items) {
  return items.filter((s) => {
    return ids.includes(s.id);
  });
}

function createUnorderedMatchingArtistSelector() {
  return createSelector(
    (state, { artistIds }) => artistIds,
    createAllArtistSelector(),
    findMatchingItems
  );
}

function createMatchingArtistSelector() {
  return createSelector(
    createUnorderedMatchingArtistSelector(),
    (artists) => {
      return artists.sort((artistA, artistB) => {
        const sortNameA = artistA.sortName;
        const sortNameB = artistB.sortName;

        if (sortNameA > sortNameB) {
          return 1;
        } else if (sortNameA < sortNameB) {
          return -1;
        }

        return 0;
      });
    }
  );
}

function createMatchingDelayProfilesSelector() {
  return createSelector(
    (state, { delayProfileIds }) => delayProfileIds,
    (state) => state.settings.delayProfiles.items,
    findMatchingItems
  );
}

function createMatchingImportListsSelector() {
  return createSelector(
    (state, { importListIds }) => importListIds,
    (state) => state.settings.importLists.items,
    findMatchingItems
  );
}

function createMatchingNotificationsSelector() {
  return createSelector(
    (state, { notificationIds }) => notificationIds,
    (state) => state.settings.notifications.items,
    findMatchingItems
  );
}

function createMatchingReleaseProfilesSelector() {
  return createSelector(
    (state, { restrictionIds }) => restrictionIds,
    (state) => state.settings.releaseProfiles.items,
    findMatchingItems
  );
}

function createMapStateToProps() {
  return createSelector(
    createMatchingArtistSelector(),
    createMatchingDelayProfilesSelector(),
    createMatchingImportListsSelector(),
    createMatchingNotificationsSelector(),
    createMatchingReleaseProfilesSelector(),
    (artist, delayProfiles, importLists, notifications, releaseProfiles) => {
      return {
        artist,
        delayProfiles,
        importLists,
        notifications,
        releaseProfiles
      };
    }
  );
}

export default connect(createMapStateToProps)(TagDetailsModalContent);
