import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import EditSelectedSongsModalContent from './EditSelectedSongsModalContent';

function EditSelectedSongsModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <EditSelectedSongsModalContent
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

EditSelectedSongsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default EditSelectedSongsModal;
