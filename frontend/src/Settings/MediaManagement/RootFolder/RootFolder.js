import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import Label from 'Components/Label';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditRootFolderModalConnector from './EditRootFolderModalConnector';
import styles from './RootFolder.css';

class RootFolder extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditRootFolderModalOpen: false,
      isDeleteRootFolderModalOpen: false
    };
  }

  //
  // Listeners

  onEditRootFolderPress = () => {
    this.setState({ isEditRootFolderModalOpen: true });
  };

  onEditRootFolderModalClose = () => {
    this.setState({ isEditRootFolderModalOpen: false });
  };

  onDeleteRootFolderPress = () => {
    this.setState({
      isEditRootFolderModalOpen: false,
      isDeleteRootFolderModalOpen: true
    });
  };

  onDeleteRootFolderModalClose= () => {
    this.setState({ isDeleteRootFolderModalOpen: false });
  };

  onConfirmDeleteRootFolder = () => {
    this.props.onConfirmDeleteRootFolder(this.props.id);
  };

  //
  // Render

  render() {
    const {
      id,
      name,
      path,
      qualityProfile,
      metadataProfile
    } = this.props;

    return (
      <Card
        className={styles.rootFolder}
        overlayContent={true}
        onPress={this.onEditRootFolderPress}
      >
        <div className={styles.name}>
          {name}
        </div>

        <div className={styles.enabled}>
          <Label kind={kinds.SUCCESS}>
            {path}
          </Label>

          <Label kind={qualityProfile?.name ? kinds.SUCCESS : kinds.DANGER}>
            {qualityProfile?.name || translate('None')}
          </Label>

          <Label kind={metadataProfile?.name ? kinds.SUCCESS : kinds.DANGER}>
            {metadataProfile?.name || translate('None')}
          </Label>
        </div>

        <EditRootFolderModalConnector
          id={id}
          isOpen={this.state.isEditRootFolderModalOpen}
          onModalClose={this.onEditRootFolderModalClose}
          onDeleteRootFolderPress={this.onDeleteRootFolderPress}
        />

        <ConfirmModal
          isOpen={this.state.isDeleteRootFolderModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteRootFolder')}
          message={translate('DeleteRootFolderMessageText', { name })}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDeleteRootFolder}
          onCancel={this.onDeleteRootFolderModalClose}
        />
      </Card>
    );
  }
}

RootFolder.propTypes = {
  id: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  path: PropTypes.string.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  metadataProfile: PropTypes.object.isRequired,
  onConfirmDeleteRootFolder: PropTypes.func.isRequired
};

export default RootFolder;
