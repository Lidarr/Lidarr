import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import ArtistTags from './ArtistTags';

function createMapStateToProps() {
  return createSelector(
    createArtistSelector(),
    createTagsSelector(),
    (artist, tagList) => {
      const tags = artist.tags
        .map((tagId) => tagList.find((tag) => tag.id === tagId))
        .filter((tag) => !!tag)
        .map((tag) => tag.label)
        .sort((a, b) => a.localeCompare(b));

      return {
        tags
      };
    }
  );
}

export default connect(createMapStateToProps)(ArtistTags);
