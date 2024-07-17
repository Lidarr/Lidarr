import { createSelector } from 'reselect';
import { RootFolderAppState } from 'App/State/SettingsAppState';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import RootFolder from 'typings/RootFolder';
import sortByProp from 'Utilities/Array/sortByProp';

export default function createRootFoldersSelector() {
  return createSelector(
    createSortedSectionSelector<RootFolder>('rootFolders', sortByProp('name')),
    (rootFolders: RootFolderAppState) => rootFolders
  );
}
