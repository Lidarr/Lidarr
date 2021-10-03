import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { icons, inputTypes, kinds } from 'Helpers/Props';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './DeleteArtistModalContent.css';

class DeleteArtistModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      deleteFiles: false,
      addImportListExclusion: false
    };
  }

  //
  // Listeners

  onDeleteFilesChange = ({ value }) => {
    this.setState({ deleteFiles: value });
  }

  onAddImportListExclusionChange = ({ value }) => {
    this.setState({ addImportListExclusion: value });
  }

  onDeleteArtistConfirmed = () => {
    const deleteFiles = this.state.deleteFiles;
    const addImportListExclusion = this.state.addImportListExclusion;

    this.setState({ deleteFiles: false });
    this.setState({ addImportListExclusion: false });
    this.props.onDeletePress(deleteFiles, addImportListExclusion);
  }

  //
  // Render

  render() {
    const {
      artistName,
      path,
      statistics,
      onModalClose
    } = this.props;

    const {
      trackFileCount,
      sizeOnDisk
    } = statistics;

    const deleteFiles = this.state.deleteFiles;
    const addImportListExclusion = this.state.addImportListExclusion;

    let deleteFilesLabel = `Delete ${trackFileCount} Track Files`;
    let deleteFilesHelpText = translate('DeleteFilesHelpText');

    if (trackFileCount === 0) {
      deleteFilesLabel = 'Delete Artist Folder';
      deleteFilesHelpText = 'Delete the artist folder and its contents';
    }

    return (
      <ModalContent
        onModalClose={onModalClose}
      >
        <ModalHeader>
          Delete - {artistName}
        </ModalHeader>

        <ModalBody>
          <div className={styles.pathContainer}>
            <Icon
              className={styles.pathIcon}
              name={icons.FOLDER}
            />

            {path}
          </div>

          <FormGroup>
            <FormLabel>{deleteFilesLabel}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="deleteFiles"
              value={deleteFiles}
              helpText={deleteFilesHelpText}
              kind={kinds.DANGER}
              onChange={this.onDeleteFilesChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>
              {translate('AddListExclusion')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="addImportListExclusion"
              value={addImportListExclusion}
              helpText={translate('AddImportListExclusionHelpText')}
              kind={kinds.DANGER}
              onChange={this.onAddImportListExclusionChange}
            />
          </FormGroup>

          {
            deleteFiles &&
              <div className={styles.deleteFilesMessage}>
                <div>
                  {translate('TheArtistFolderStrongpathstrongAndAllOfItsContentWillBeDeleted')}
                </div>

                {
                  !!trackFileCount &&
                    <div>{trackFileCount} track files totaling {formatBytes(sizeOnDisk)}</div>
                }
              </div>
          }

        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            Close
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
  artistName: PropTypes.string.isRequired,
  path: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  onDeletePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

DeleteArtistModalContent.defaultProps = {
  statistics: {
    trackFileCount: 0
  }
};

export default DeleteArtistModalContent;
