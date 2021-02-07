import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import AlbumCover from 'Album/AlbumCover';
import CheckInput from 'Components/Form/CheckInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AddArtistOptionsForm from '../Common/AddArtistOptionsForm.js';
import styles from './AddNewAlbumModalContent.css';

class AddNewAlbumModalContent extends Component {

  //
  // Listeners

  onAddAlbumPress = () => {
    this.props.onAddAlbumPress();
  };

  //
  // Render

  render() {
    const {
      albumTitle,
      artistName,
      disambiguation,
      overview,
      images,
      searchForNewAlbum,
      isAdding,
      isExistingArtist,
      isSmallScreen,
      onModalClose,
      onInputChange,
      ...otherProps
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('AddNewAlbum')}
        </ModalHeader>

        <ModalBody>
          <div className={styles.container}>
            {
              isSmallScreen ?
                null:
                <div className={styles.poster}>
                  <AlbumCover
                    className={styles.poster}
                    images={images}
                    size={250}
                  />
                </div>
            }

            <div className={styles.info}>
              <div className={styles.name}>
                {albumTitle}
              </div>

              {
                !!disambiguation &&
                  <span className={styles.disambiguation}>({disambiguation})</span>
              }

              <div>
                <span className={styles.artistName}> By: {artistName}</span>
              </div>

              {
                overview ?
                  <div className={styles.overview}>
                    <TextTruncate
                      truncateText="…"
                      line={8}
                      text={overview}
                    />
                  </div> :
                  null
              }

              {
                !isExistingArtist &&
                  <AddArtistOptionsForm
                    artistName={artistName}
                    includeNoneMetadataProfile={true}
                    onInputChange={onInputChange}
                    {...otherProps}
                  />
              }
            </div>
          </div>
        </ModalBody>

        <ModalFooter className={styles.modalFooter}>
          <label className={styles.searchForNewAlbumLabelContainer}>
            <span className={styles.searchForNewAlbumLabel}>
              {translate('AddNewAlbumSearchForNewAlbum')}
            </span>

            <CheckInput
              containerClassName={styles.searchForNewAlbumContainer}
              className={styles.searchForNewAlbumInput}
              name="searchForNewAlbum"
              onChange={onInputChange}
              {...searchForNewAlbum}
            />
          </label>

          <SpinnerButton
            className={styles.addButton}
            kind={kinds.SUCCESS}
            isSpinning={isAdding}
            onPress={this.onAddAlbumPress}
          >
            {translate('AddAlbumWithTitle', { albumTitle })}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AddNewAlbumModalContent.propTypes = {
  albumTitle: PropTypes.string.isRequired,
  artistName: PropTypes.string.isRequired,
  disambiguation: PropTypes.string.isRequired,
  overview: PropTypes.string,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  isAdding: PropTypes.bool.isRequired,
  addError: PropTypes.object,
  searchForNewAlbum: PropTypes.object.isRequired,
  isExistingArtist: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onAddAlbumPress: PropTypes.func.isRequired
};

export default AddNewAlbumModalContent;
