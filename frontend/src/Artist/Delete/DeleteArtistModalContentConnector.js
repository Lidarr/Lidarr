import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteArtist, setDeleteOption } from 'Store/Actions/artistActions';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import DeleteArtistModalContent from './DeleteArtistModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.artist.deleteOptions,
    createArtistSelector(),
    (deleteOptions, artist) => {
      return {
        ...artist,
        deleteOptions
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onDeleteOptionChange(option) {
      dispatch(
        setDeleteOption({
          [option.name]: option.value
        })
      );
    },

    onDeletePress(deleteFiles, addImportListExclusion) {
      dispatch(
        deleteArtist({
          id: props.artistId,
          deleteFiles,
          addImportListExclusion
        })
      );

      props.onModalClose(true);
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DeleteArtistModalContent);
