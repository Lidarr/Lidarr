import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Modal from 'Components/Modal/Modal';
import SelectIndexerFlagsModalContentConnector from './SelectIndexerFlagsModalContentConnector';

class SelectIndexerFlagsModal extends Component {

  //
  // Render

  render() {
    const {
      isOpen,
      onModalClose,
      ...otherProps
    } = this.props;

    return (
      <Modal
        isOpen={isOpen}
        onModalClose={onModalClose}
      >
        <SelectIndexerFlagsModalContentConnector
          {...otherProps}
          onModalClose={onModalClose}
        />
      </Modal>
    );
  }
}

SelectIndexerFlagsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default SelectIndexerFlagsModal;
