import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import styles from './DeleteArtistModalContent.css';

class DeleteArtistModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      deleteFiles: false
    };
  }

  //
  // Listeners

  onDeleteFilesChange = ({ value }) => {
    this.setState({ deleteFiles: value });
  }

  onDeleteArtistConfirmed = () => {
    const deleteFiles = this.state.deleteFiles;

    this.setState({ deleteFiles: false });
    this.props.onDeleteSelectedPress(deleteFiles);
  }

  //
  // Render

  render() {
    const {
      artist,
      onModalClose
    } = this.props;
    const deleteFiles = this.state.deleteFiles;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Delete Selected Artist
        </ModalHeader>

        <ModalBody>
          <div>
            <FormGroup>
              <FormLabel>{`Delete Artist Folder${artist.length > 1 ? 's' : ''}`}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="deleteFiles"
                value={deleteFiles}
                helpText={`Delete Artist Folder${artist.length > 1 ? 's' : ''} and all contents`}
                kind={kinds.DANGER}
                onChange={this.onDeleteFilesChange}
              />
            </FormGroup>
          </div>

          <div className={styles.message}>
            {`Are you sure you want to delete ${artist.length} selected artist${artist.length > 1 ? 's' : ''}${deleteFiles ? ' and all contents' : ''}?`}
          </div>

          <ul>
            {
              artist.map((s) => {
                return (
                  <li key={s.artistName}>
                    <span>{s.artistName}</span>

                    {
                      deleteFiles &&
                        <span className={styles.pathContainer}>
                          -
                          <span className={styles.path}>
                            {s.path}
                          </span>
                        </span>
                    }
                  </li>
                );
              })
            }
          </ul>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            Cancel
          </Button>

          <Button
            kind={kinds.DANGER}
            onPress={this.onDeleteArtistConfirmed}
          >
            Delete
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

DeleteArtistModalContent.propTypes = {
  artist: PropTypes.arrayOf(PropTypes.object).isRequired,
  onModalClose: PropTypes.func.isRequired,
  onDeleteSelectedPress: PropTypes.func.isRequired
};

export default DeleteArtistModalContent;
