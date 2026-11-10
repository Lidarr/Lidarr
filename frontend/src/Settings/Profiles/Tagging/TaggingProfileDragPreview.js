import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { DragLayer } from 'react-dnd';
import DragPreviewLayer from 'Components/DragPreviewLayer';
import { TAGGING_PROFILE } from 'Helpers/dragTypes';
import dimensions from 'Styles/Variables/dimensions.js';
import TaggingProfile from './TaggingProfile';
import styles from './TaggingProfileDragPreview.css';

const dragHandleWidth = parseInt(dimensions.dragHandleWidth);

function collectDragLayer(monitor) {
  return {
    item: monitor.getItem(),
    itemType: monitor.getItemType(),
    currentOffset: monitor.getSourceClientOffset()
  };
}

class TaggingProfileDragPreview extends Component {

  //
  // Render

  render() {
    const {
      width,
      item,
      itemType,
      currentOffset
    } = this.props;

    if (!currentOffset || itemType !== TAGGING_PROFILE) {
      return null;
    }

    // The offset is shifted because the drag handle is on the right edge of the
    // list item and the preview is wider than the drag handle.

    const { x, y } = currentOffset;
    const handleOffset = width - dragHandleWidth;
    const transform = `translate3d(${x - handleOffset}px, ${y}px, 0)`;

    const style = {
      width,
      position: 'absolute',
      WebkitTransform: transform,
      msTransform: transform,
      transform
    };

    return (
      <DragPreviewLayer>
        <div
          className={styles.dragPreview}
          style={style}
        >
          <TaggingProfile
            isDragging={false}
            {...item}
          />
        </div>
      </DragPreviewLayer>
    );
  }
}

TaggingProfileDragPreview.propTypes = {
  width: PropTypes.number.isRequired,
  item: PropTypes.object,
  itemType: PropTypes.string,
  currentOffset: PropTypes.shape({
    x: PropTypes.number.isRequired,
    y: PropTypes.number.isRequired
  })
};

export default DragLayer(collectDragLayer)(TaggingProfileDragPreview);
