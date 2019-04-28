import { connect } from 'react-redux';
import { push } from 'connected-react-router';
import { createSelector } from 'reselect';
import createAllArtistSelector from 'Store/Selectors/createAllArtistSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import ArtistSearchInput from './ArtistSearchInput';

function createCleanArtistSelector() {
  return createSelector(
    createAllArtistSelector(),
    createTagsSelector(),
    (allArtists, allTags) => {
      return allArtists.map((artist) => {
        const {
          artistName,
          sortName,
          images,
          foreignArtistId,
          // alternateTitles,
          tags = []
        } = artist;

        return {
          artistName,
          sortName,
          foreignArtistId,
          images,
          tags: tags.map((id) => {
            return allTags.find((tag) => tag.id === id);
          })
        };
      });
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createCleanArtistSelector(),
    (artists) => {
      return {
        artists
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onGoToArtist(foreignArtistId) {
      dispatch(push(`${window.Lidarr.urlBase}/artist/${foreignArtistId}`));
    },

    onGoToAddNewArtist(query) {
      dispatch(push(`${window.Lidarr.urlBase}/add/new?term=${encodeURIComponent(query)}`));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(ArtistSearchInput);
