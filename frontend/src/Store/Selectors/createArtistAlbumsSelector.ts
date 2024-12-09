import { createSelector } from 'reselect';
import AlbumAppState from 'App/State/AlbumAppState';
import AppState from 'App/State/AppState';
import Artist from 'Artist/Artist';
import { createArtistSelectorForHook } from './createArtistSelector';

function createArtistAlbumsSelector(artistId: number) {
  return createSelector(
    (state: AppState) => state.albums,
    createArtistSelectorForHook(artistId),
    (albums: AlbumAppState, artist = {} as Artist) => {
      const { isFetching, isPopulated, error, items } = albums;

      const filteredAlbums = items.filter(
        (album) => album.artistId === artist.id
      );

      return {
        isFetching,
        isPopulated,
        error,
        items: filteredAlbums,
      };
    }
  );
}

export default createArtistAlbumsSelector;
