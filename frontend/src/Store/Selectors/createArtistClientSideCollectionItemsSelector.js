import { createSelector, createSelectorCreator, defaultMemoize } from 'reselect';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

function createUnoptimizedSelector(uiSection) {
  return createSelector(
    createClientSideCollectionSelector('artist', uiSection),
    (artist) => {
      const items = artist.items.map((s) => {
        const {
          id,
          sortName
        } = s;

        return {
          id,
          sortName
        };
      });

      return {
        ...artist,
        items
      };
    }
  );
}

function artistListEqual(a, b) {
  return hasDifferentItemsOrOrder(a, b);
}

const createArtistEqualSelector = createSelectorCreator(
  defaultMemoize,
  artistListEqual
);

function createArtistClientSideCollectionItemsSelector(uiSection) {
  return createArtistEqualSelector(
    createUnoptimizedSelector(uiSection),
    (artist) => artist
  );
}

export default createArtistClientSideCollectionItemsSelector;
