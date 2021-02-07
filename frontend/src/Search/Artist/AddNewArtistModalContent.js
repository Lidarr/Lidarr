import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import ArtistPoster from 'Artist/ArtistPoster';
import CheckInput from 'Components/Form/CheckInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AddArtistOptionsForm from '../Common/AddArtistOptionsForm.js';
import styles from './AddNewArtistModalContent.css';

class AddNewArtistModalContent extends Component {

  //
  // Listeners

  onAddArtistPress = () => {
    this.props.onAddArtistPress();
  };

  //
  // Render

  render() {
    const {
      artistName,
      disambiguation,
      overview,
      images,
      searchForMissingAlbums,
      isAdding,
      isSmallScreen,
      onModalClose,
      onInputChange,
      ...otherProps
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('AddNewArtist')}
        </ModalHeader>

        <ModalBody>
          <div className={styles.container}>
            {
              isSmallScreen ?
                null:
                <div className={styles.poster}>
                  <ArtistPoster
                    className={styles.poster}
                    images={images}
                    size={250}
                  />
                </div>
            }

            <div className={styles.info}>
              <div className={styles.name}>
                {artistName}
              </div>

              {
                !!disambiguation &&
                  <span className={styles.disambiguation}>({disambiguation})</span>
              }

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

              <AddArtistOptionsForm
                includeNoneMetadataProfile={false}
                onInputChange={onInputChange}
                {...otherProps}
              />

            </div>
          </div>
        </ModalBody>

        <ModalFooter className={styles.modalFooter}>
          <label className={styles.searchForMissingAlbumsLabelContainer}>
            <span className={styles.searchForMissingAlbumsLabel}>
              {translate('AddNewArtistSearchForMissingAlbums')}
            </span>

            <CheckInput
              containerClassName={styles.searchForMissingAlbumsContainer}
              className={styles.searchForMissingAlbumsInput}
              name="searchForMissingAlbums"
              onChange={onInputChange}
              {...searchForMissingAlbums}
            />
          </label>

          <SpinnerButton
            className={styles.addButton}
            kind={kinds.SUCCESS}
            isSpinning={isAdding}
            onPress={this.onAddArtistPress}
          >
            {translate('AddArtistWithName', { artistName })}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AddNewArtistModalContent.propTypes = {
  artistName: PropTypes.string.isRequired,
  disambiguation: PropTypes.string.isRequired,
  overview: PropTypes.string,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  isAdding: PropTypes.bool.isRequired,
  addError: PropTypes.object,
  searchForMissingAlbums: PropTypes.object.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  isWindows: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onAddArtistPress: PropTypes.func.isRequired
};

export default AddNewArtistModalContent;
