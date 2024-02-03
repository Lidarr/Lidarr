import { createSelector } from 'reselect';
import { RootFolderAppState } from 'App/State/SettingsAppState';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import sortByName from 'Utilities/Array/sortByName';

export default function createRootFoldersSelector() {
  return createSelector(
    createSortedSectionSelector('settings.rootFolders', sortByName),
    (rootFolders: RootFolderAppState) => rootFolders
  );
}
