import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import Label from 'Components/Label';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TagList from 'Components/TagList';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditNotificationModalConnector from './EditNotificationModalConnector';
import styles from './Notification.css';

class Notification extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditNotificationModalOpen: false,
      isDeleteNotificationModalOpen: false
    };
  }

  //
  // Listeners

  onEditNotificationPress = () => {
    this.setState({ isEditNotificationModalOpen: true });
  };

  onEditNotificationModalClose = () => {
    this.setState({ isEditNotificationModalOpen: false });
  };

  onDeleteNotificationPress = () => {
    this.setState({
      isEditNotificationModalOpen: false,
      isDeleteNotificationModalOpen: true
    });
  };

  onDeleteNotificationModalClose= () => {
    this.setState({ isDeleteNotificationModalOpen: false });
  };

  onConfirmDeleteNotification = () => {
    this.props.onConfirmDeleteNotification(this.props.id);
  };

  //
  // Render

  render() {
    const {
      id,
      name,
      onGrab,
      onReleaseImport,
      onUpgrade,
      onRename,
      onArtistAdd,
      onArtistDelete,
      onAlbumDelete,
      onHealthIssue,
      onHealthRestored,
      onDownloadFailure,
      onImportFailure,
      onTrackRetag,
      onApplicationUpdate,
      supportsOnGrab,
      supportsOnReleaseImport,
      supportsOnUpgrade,
      supportsOnRename,
      supportsOnArtistAdd,
      supportsOnArtistDelete,
      supportsOnAlbumDelete,
      supportsOnHealthIssue,
      supportsOnHealthRestored,
      supportsOnDownloadFailure,
      supportsOnImportFailure,
      supportsOnTrackRetag,
      supportsOnApplicationUpdate,
      tags,
      tagList
    } = this.props;

    return (
      <Card
        className={styles.notification}
        overlayContent={true}
        onPress={this.onEditNotificationPress}
      >
        <div className={styles.name}>
          {name}
        </div>

        {
          supportsOnGrab && onGrab ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnGrab')}
            </Label> :
            null
        }

        {
          supportsOnReleaseImport && onReleaseImport ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnReleaseImport')}
            </Label> :
            null
        }

        {
          supportsOnUpgrade && onReleaseImport && onUpgrade ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnUpgrade')}
            </Label> :
            null
        }

        {
          supportsOnRename && onRename ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnRename')}
            </Label> :
            null
        }

        {
          supportsOnTrackRetag && onTrackRetag ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnTrackRetag')}
            </Label> :
            null
        }

        {
          supportsOnArtistAdd && onArtistAdd ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnArtistAdd')}
            </Label> :
            null
        }

        {
          supportsOnArtistDelete && onArtistDelete ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnArtistDelete')}
            </Label> :
            null
        }

        {
          supportsOnAlbumDelete && onAlbumDelete ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnAlbumDelete')}
            </Label> :
            null
        }

        {
          supportsOnHealthIssue && onHealthIssue ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnHealthIssue')}
            </Label> :
            null
        }

        {
          supportsOnHealthRestored && onHealthRestored ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnHealthRestored')}
            </Label> :
            null
        }

        {
          supportsOnDownloadFailure && onDownloadFailure ?
            <Label kind={kinds.SUCCESS} >
              {translate('OnDownloadFailure')}
            </Label> :
            null
        }

        {
          supportsOnImportFailure && onImportFailure ?
            <Label kind={kinds.SUCCESS} >
              {translate('OnImportFailure')}
            </Label> :
            null
        }

        {
          supportsOnApplicationUpdate && onApplicationUpdate ?
            <Label kind={kinds.SUCCESS} >
              {translate('OnApplicationUpdate')}
            </Label> :
            null
        }

        {
          !onGrab && !onReleaseImport && !onRename && !onTrackRetag && !onArtistAdd && !onArtistDelete && !onAlbumDelete && !onHealthIssue && !onHealthRestored && !onDownloadFailure && !onImportFailure && !onApplicationUpdate ?
            <Label
              kind={kinds.DISABLED}
              outline={true}
            >
              {translate('Disabled')}
            </Label> :
            null
        }

        <TagList
          tags={tags}
          tagList={tagList}
        />

        <EditNotificationModalConnector
          id={id}
          isOpen={this.state.isEditNotificationModalOpen}
          onModalClose={this.onEditNotificationModalClose}
          onDeleteNotificationPress={this.onDeleteNotificationPress}
        />

        <ConfirmModal
          isOpen={this.state.isDeleteNotificationModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteNotification')}
          message={translate('DeleteNotificationMessageText', { name })}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDeleteNotification}
          onCancel={this.onDeleteNotificationModalClose}
        />
      </Card>
    );
  }
}

Notification.propTypes = {
  id: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  onGrab: PropTypes.bool.isRequired,
  onReleaseImport: PropTypes.bool.isRequired,
  onUpgrade: PropTypes.bool.isRequired,
  onRename: PropTypes.bool.isRequired,
  onArtistAdd: PropTypes.bool.isRequired,
  onArtistDelete: PropTypes.bool.isRequired,
  onAlbumDelete: PropTypes.bool.isRequired,
  onHealthIssue: PropTypes.bool.isRequired,
  onHealthRestored: PropTypes.bool.isRequired,
  onDownloadFailure: PropTypes.bool.isRequired,
  onImportFailure: PropTypes.bool.isRequired,
  onTrackRetag: PropTypes.bool.isRequired,
  onApplicationUpdate: PropTypes.bool.isRequired,
  supportsOnGrab: PropTypes.bool.isRequired,
  supportsOnReleaseImport: PropTypes.bool.isRequired,
  supportsOnUpgrade: PropTypes.bool.isRequired,
  supportsOnRename: PropTypes.bool.isRequired,
  supportsOnArtistAdd: PropTypes.bool.isRequired,
  supportsOnArtistDelete: PropTypes.bool.isRequired,
  supportsOnAlbumDelete: PropTypes.bool.isRequired,
  supportsOnHealthIssue: PropTypes.bool.isRequired,
  supportsOnHealthRestored: PropTypes.bool.isRequired,
  supportsOnDownloadFailure: PropTypes.bool.isRequired,
  supportsOnImportFailure: PropTypes.bool.isRequired,
  supportsOnTrackRetag: PropTypes.bool.isRequired,
  supportsOnApplicationUpdate: PropTypes.bool.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  tagList: PropTypes.arrayOf(PropTypes.object).isRequired,
  onConfirmDeleteNotification: PropTypes.func.isRequired
};

export default Notification;
