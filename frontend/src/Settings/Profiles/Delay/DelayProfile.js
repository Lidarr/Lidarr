import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TagList from 'Components/TagList';
import { icons, kinds } from 'Helpers/Props';
import EditDelayProfileModalConnector from './EditDelayProfileModalConnector';
import styles from './DelayProfile.css';

function getDelay(item) {
  if (!item.allowed) {
    return '-';
  }

  if (!item.delay) {
    return 'No Delay';
  }

  if (item.delay === 1) {
    return '1 Minute';
  }

  // TODO: use better units of time than just minutes
  return `${item.delay} Minutes`;
}

class DelayProfile extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditDelayProfileModalOpen: false,
      isDeleteDelayProfileModalOpen: false
    };
  }

  //
  // Listeners

  onEditDelayProfilePress = () => {
    this.setState({ isEditDelayProfileModalOpen: true });
  }

  onEditDelayProfileModalClose = () => {
    this.setState({ isEditDelayProfileModalOpen: false });
  }

  onDeleteDelayProfilePress = () => {
    this.setState({
      isEditDelayProfileModalOpen: false,
      isDeleteDelayProfileModalOpen: true
    });
  }

  onDeleteDelayProfileModalClose = () => {
    this.setState({ isDeleteDelayProfileModalOpen: false });
  }

  onConfirmDeleteDelayProfile = () => {
    this.props.onConfirmDeleteDelayProfile(this.props.id);
  }

  //
  // Render

  render() {
    const {
      id,
      name,
      items,
      tags,
      tagList,
      isDragging,
      connectDragSource
    } = this.props;

    const usenet = items.find((x) => x.protocol === 'usenet');
    const torrent = items.find((x) => x.protocol === 'torrent');
    const deemix = items.find((x) => x.protocol === 'deemix');

    return (
      <div
        className={classNames(
          styles.delayProfile,
          isDragging && styles.isDragging
        )}
      >

        <div className={styles.column}>{name}</div>

        <div className={styles.column}>
          {
            items.map((x) => {
              return (
                <Label
                  key={x.id}
                  kind={x.allowed ? kinds.INFO : kinds.DANGER}
                >
                  {x.name}
                </Label>
              );
            })
          }
        </div>

        <div className={styles.column}>{getDelay(usenet)}</div>
        <div className={styles.column}>{getDelay(torrent)}</div>
        <div className={styles.column}>{getDelay(deemix)}</div>

        <TagList
          tags={tags}
          tagList={tagList}
        />

        <div className={styles.actions}>
          <Link
            className={id === 1 ? styles.editButton : undefined}
            onPress={this.onEditDelayProfilePress}
          >
            <Icon name={icons.EDIT} />
          </Link>

          {
            id !== 1 &&
              connectDragSource(
                <div className={styles.dragHandle}>
                  <Icon
                    className={styles.dragIcon}
                    name={icons.REORDER}
                  />
                </div>
              )
          }
        </div>

        <EditDelayProfileModalConnector
          id={id}
          isOpen={this.state.isEditDelayProfileModalOpen}
          onModalClose={this.onEditDelayProfileModalClose}
          onDeleteDelayProfilePress={this.onDeleteDelayProfilePress}
        />

        <ConfirmModal
          isOpen={this.state.isDeleteDelayProfileModalOpen}
          kind={kinds.DANGER}
          title="Delete Delay Profile"
          message="Are you sure you want to delete this delay profile?"
          confirmLabel="Delete"
          onConfirm={this.onConfirmDeleteDelayProfile}
          onCancel={this.onDeleteDelayProfileModalClose}
        />
      </div>
    );
  }
}

DelayProfile.propTypes = {
  id: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  tagList: PropTypes.arrayOf(PropTypes.object).isRequired,
  isDragging: PropTypes.bool.isRequired,
  connectDragSource: PropTypes.func,
  onConfirmDeleteDelayProfile: PropTypes.func.isRequired
};

DelayProfile.defaultProps = {
  // The drag preview will not connect the drag handle.
  connectDragSource: (node) => node
};

export default DelayProfile;
