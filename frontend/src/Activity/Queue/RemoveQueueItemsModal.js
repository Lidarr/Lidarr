import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import styles from './RemoveQueueItemsModal.css';

class RemoveQueueItemsModal extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      remove: true,
      blocklist: false,
      skipredownload: false
    };
  }

  //
  // Control

   resetState = function() {
     this.setState({
       remove: true,
       blocklist: false,
       skipredownload: false
     });
   }

   //
   // Listeners

   onRemoveChange = ({ value }) => {
     this.setState({ remove: value });
   }

  onBlocklistChange = ({ value }) => {
    this.setState({ blocklist: value });
  }

  onSkipReDownloadChange = ({ value }) => {
    this.setState({ skipredownload: value });
  }

  onRemoveConfirmed = () => {
    const state = this.state;

    this.resetState();
    this.props.onRemovePress(state);
  }

  onModalClose = () => {
    this.resetState();
    this.props.onModalClose();
  }

  //
  // Render

  render() {
    const {
      isOpen,
      selectedCount,
      canIgnore
    } = this.props;

    const { remove, blocklist, skipredownload } = this.state;

    return (
      <Modal
        isOpen={isOpen}
        size={sizes.MEDIUM}
        onModalClose={this.onModalClose}
      >
        <ModalContent
          onModalClose={this.onModalClose}
        >
          <ModalHeader>
            Remove Selected Item{selectedCount > 1 ? 's' : ''}
          </ModalHeader>

          <ModalBody>
            <div className={styles.message}>
              Are you sure you want to remove {selectedCount} item{selectedCount > 1 ? 's' : ''} from the queue?
            </div>

            <FormGroup>
              <FormLabel>Remove From Download Client</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="remove"
                value={remove}
                helpTextWarning="Removing will remove the download and the file(s) from the download client."
                isDisabled={!canIgnore}
                onChange={this.onRemoveChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                Blocklist Release{selectedCount > 1 ? 's' : ''}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="blocklist"
                value={blocklist}
                helpText="Prevents Lidarr from automatically grabbing these files again"
                onChange={this.onBlocklistChange}
              />
            </FormGroup>

            {
              blocklist &&
                <FormGroup>
                  <FormLabel>Skip Redownload</FormLabel>
                  <FormInputGroup
                    type={inputTypes.CHECK}
                    name="skipredownload"
                    value={skipredownload}
                    helpText="Prevents Lidarr from trying download alternative releases for the removed items"
                    onChange={this.onSkipReDownloadChange}
                  />
                </FormGroup>
            }

          </ModalBody>

          <ModalFooter>
            <Button onPress={this.onModalClose}>
              Close
            </Button>

            <Button
              kind={kinds.DANGER}
              onPress={this.onRemoveConfirmed}
            >
              Remove
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    );
  }
}

RemoveQueueItemsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  selectedCount: PropTypes.number.isRequired,
  canIgnore: PropTypes.bool.isRequired,
  onRemovePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default RemoveQueueItemsModal;
