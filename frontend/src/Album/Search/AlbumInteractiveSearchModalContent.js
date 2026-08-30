import PropTypes from 'prop-types';
import React from 'react';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { scrollDirections } from 'Helpers/Props';
import InteractiveSearchConnector from 'InteractiveSearch/InteractiveSearchConnector';
import translate from 'Utilities/String/translate';

function AlbumInteractiveSearchModalContent(props) {
  const {
    albumId,
    albumTitle,
    trackId,
    onModalClose
  } = props;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {albumTitle === undefined ?
          translate('InteractiveSearchModalHeader') :
          translate('InteractiveSearchModalHeaderTitle', { title: albumTitle })
        }
      </ModalHeader>

      <ModalBody scrollDirection={scrollDirections.BOTH}>
        <InteractiveSearchConnector
          type="album"
          searchPayload={{
            albumId,
            trackId
          }}
        />
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>
          {translate('Close')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

AlbumInteractiveSearchModalContent.propTypes = {
  albumId: PropTypes.number.isRequired,
  albumTitle: PropTypes.string.isRequired,
  trackId: PropTypes.number,
  onModalClose: PropTypes.func.isRequired
};

export default AlbumInteractiveSearchModalContent;
