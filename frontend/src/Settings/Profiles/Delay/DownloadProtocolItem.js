import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import CheckInput from 'Components/Form/CheckInput';
import NumberInput from 'Components/Form/NumberInput';
import Icon from 'Components/Icon';
import { icons } from 'Helpers/Props';
import styles from './DownloadProtocolItem.css';

class DownloadProtocolItem extends Component {

  //
  // Listeners

  onChange = ({ name, value }) => {
    const {
      protocol,
      onDownloadProtocolItemFieldChange
    } = this.props;

    onDownloadProtocolItemFieldChange(protocol, name, value);
  };

  //
  // Render

  render() {
    const {
      isPreview,
      name,
      allowed,
      delay,
      isDragging,
      isOverCurrent,
      connectDragSource
    } = this.props;

    return (
      <div
        className={classNames(
          styles.qualityProfileItem,
          isDragging && styles.isDragging,
          isPreview && styles.isPreview,
          isOverCurrent && styles.isOverCurrent
        )}
      >
        <label
          className={styles.qualityNameContainer}
        >

          <CheckInput
            className={styles.checkInput}
            containerClassName={styles.checkInputContainer}
            name={'allowed'}
            value={allowed}
            onChange={this.onChange}
          />

          <div className={classNames(
            styles.qualityName,
            !allowed && styles.notAllowed
          )}
          >
            {name}
          </div>
        </label>

        <NumberInput
          containerClassName={styles.delayContainer}
          className={styles.delayInput}
          name={'delay'}
          value={delay}
          min={0}
          max={9999999}
          onChange={this.onChange}
        />

        {
          connectDragSource(
            <div className={styles.dragHandle}>
              <Icon
                className={styles.dragIcon}
                title="Create group"
                name={icons.REORDER}
              />
            </div>
          )
        }
      </div>
    );
  }
}

DownloadProtocolItem.propTypes = {
  isPreview: PropTypes.bool,
  protocol: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
  allowed: PropTypes.bool.isRequired,
  delay: PropTypes.number.isRequired,
  isDragging: PropTypes.bool.isRequired,
  isOverCurrent: PropTypes.bool.isRequired,
  connectDragSource: PropTypes.func,
  onDownloadProtocolItemFieldChange: PropTypes.func
};

DownloadProtocolItem.defaultProps = {
  isPreview: false,
  isOverCurrent: false,
  // The drag preview will not connect the drag handle.
  connectDragSource: (node) => node
};

export default DownloadProtocolItem;
