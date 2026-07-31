import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import EditDownloadClientAudioTagsModalContent from './EditDownloadClientAudioTagsModalContent';

function EditDownloadClientAudioTagsModal({ isOpen, onModalClose }) {
  return (
    <Modal
      size={sizes.MEDIUM}
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <EditDownloadClientAudioTagsModalContent
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

EditDownloadClientAudioTagsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default EditDownloadClientAudioTagsModal;
