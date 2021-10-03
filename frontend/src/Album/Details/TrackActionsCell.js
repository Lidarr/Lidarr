import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons, kinds } from 'Helpers/Props';
import FileDetailsModal from 'TrackFile/FileDetailsModal';
import translate from 'Utilities/String/translate';
import styles from './TrackActionsCell.css';

class TrackActionsCell extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false,
      isConfirmDeleteModalOpen: false
    };
  }

  //
  // Listeners

  onDetailsPress = () => {
    this.setState({ isDetailsModalOpen: true });
  }

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  }

  onDeleteFilePress = () => {
    this.setState({ isConfirmDeleteModalOpen: true });
  }

  onConfirmDelete = () => {
    this.setState({ isConfirmDeleteModalOpen: false });
    this.props.deleteTrackFile({ id: this.props.trackFileId });
  }

  onConfirmDeleteModalClose = () => {
    this.setState({ isConfirmDeleteModalOpen: false });
  }

  //
  // Render

  render() {

    const {
      trackFileId,
      trackFilePath
    } = this.props;

    const {
      isDetailsModalOpen,
      isConfirmDeleteModalOpen
    } = this.state;

    return (
      <TableRowCell className={styles.TrackActionsCell}>
        {
          trackFilePath &&
            <IconButton
              name={icons.INFO}
              onPress={this.onDetailsPress}
            />
        }
        {
          trackFilePath &&
            <IconButton
              name={icons.DELETE}
              onPress={this.onDeleteFilePress}
            />
        }

        <FileDetailsModal
          isOpen={isDetailsModalOpen}
          onModalClose={this.onDetailsModalClose}
          id={trackFileId}
        />

        <ConfirmModal
          isOpen={isConfirmDeleteModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteTrackFile')}
          message={translate('DeleteTrackFileMessageText', [trackFilePath])}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDelete}
          onCancel={this.onConfirmDeleteModalClose}
        />
      </TableRowCell>

    );
  }
}

TrackActionsCell.propTypes = {
  id: PropTypes.number.isRequired,
  albumId: PropTypes.number.isRequired,
  trackFilePath: PropTypes.string,
  trackFileId: PropTypes.number.isRequired,
  deleteTrackFile: PropTypes.func.isRequired
};

export default TrackActionsCell;
