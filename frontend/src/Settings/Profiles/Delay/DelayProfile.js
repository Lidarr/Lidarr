import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TagList from 'Components/TagList';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import DelayProfileItem from './DelayProfileItem';
import EditDelayProfileModalConnector from './EditDelayProfileModalConnector';
import styles from './DelayProfile.css';

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
  };

  onEditDelayProfileModalClose = () => {
    this.setState({ isEditDelayProfileModalOpen: false });
  };

  onDeleteDelayProfilePress = () => {
    this.setState({
      isEditDelayProfileModalOpen: false,
      isDeleteDelayProfileModalOpen: true
    });
  };

  onDeleteDelayProfileModalClose = () => {
    this.setState({ isDeleteDelayProfileModalOpen: false });
  };

  onConfirmDeleteDelayProfile = () => {
    this.props.onConfirmDeleteDelayProfile(this.props.id);
  };

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

    return (
      <div
        className={classNames(
          styles.delayProfile,
          isDragging && styles.isDragging
        )}
      >

        <div className={styles.name}>{name}</div>

        <div className={styles.fillcolumn}>
          {
            items.map((x) => {
              return (
                <DelayProfileItem
                  key={x.protocol}
                  name={x.name}
                  allowed={x.allowed}
                  delay={x.delay}
                />
              );
            })
          }
        </div>

        <TagList
          className={styles.fillcolumn}
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
          title={translate('DeleteDelayProfile')}
          message={translate('DeleteDelayProfileMessageText')}
          confirmLabel={translate('Delete')}
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
