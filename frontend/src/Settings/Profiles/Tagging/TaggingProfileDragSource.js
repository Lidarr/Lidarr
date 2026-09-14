import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { DragSource, DropTarget } from 'react-dnd';
import { findDOMNode } from 'react-dom';
import { TAGGING_PROFILE } from 'Helpers/dragTypes';
import TaggingProfile from './TaggingProfile';
import styles from './TaggingProfileDragSource.css';

const taggingProfileDragSource = {
  beginDrag(item) {
    return item;
  },

  endDrag(props, monitor, component) {
    props.onTaggingProfileDragEnd(monitor.getItem(), monitor.didDrop());
  }
};

const taggingProfileDropTarget = {
  hover(props, monitor, component) {
    const dragIndex = monitor.getItem().order;
    const hoverIndex = props.order;

    const hoverBoundingRect = findDOMNode(component).getBoundingClientRect();
    const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
    const clientOffset = monitor.getClientOffset();
    const hoverClientY = clientOffset.y - hoverBoundingRect.top;

    if (dragIndex === hoverIndex) {
      return;
    }

    // When moving up, only trigger if drag position is above 50% and
    // when moving down, only trigger if drag position is below 50%.
    // If we're moving down the hoverIndex needs to be increased
    // by one so it's ordered properly. Otherwise the hoverIndex will work.

    if (dragIndex < hoverIndex && hoverClientY > hoverMiddleY) {
      props.onTaggingProfileDragMove(dragIndex, hoverIndex + 1);
    } else if (dragIndex > hoverIndex && hoverClientY < hoverMiddleY) {
      props.onTaggingProfileDragMove(dragIndex, hoverIndex);
    }
  }
};

function collectDragSource(connect, monitor) {
  return {
    connectDragSource: connect.dragSource(),
    isDragging: monitor.isDragging()
  };
}

function collectDropTarget(connect, monitor) {
  return {
    connectDropTarget: connect.dropTarget(),
    isOver: monitor.isOver()
  };
}

class TaggingProfileDragSource extends Component {

  //
  // Render

  render() {
    const {
      id,
      order,
      isDragging,
      isDraggingUp,
      isDraggingDown,
      isOver,
      connectDragSource,
      connectDropTarget,
      ...otherProps
    } = this.props;

    const isBefore = !isDragging && isDraggingUp && isOver;
    const isAfter = !isDragging && isDraggingDown && isOver;

    return connectDropTarget(
      <div
        className={classNames(
          styles.taggingProfileDragSource,
          isBefore && styles.isDraggingUp,
          isAfter && styles.isDraggingDown
        )}
      >
        {
          isBefore &&
            <div
              className={classNames(
                styles.taggingProfilePlaceholder,
                styles.taggingProfilePlaceholderBefore
              )}
            />
        }

        <TaggingProfile
          id={id}
          order={order}
          isDragging={isDragging}
          {...otherProps}
          connectDragSource={connectDragSource}
        />

        {
          isAfter &&
            <div
              className={classNames(
                styles.taggingProfilePlaceholder,
                styles.taggingProfilePlaceholderAfter
              )}
            />
        }
      </div>
    );
  }
}

TaggingProfileDragSource.propTypes = {
  id: PropTypes.number.isRequired,
  order: PropTypes.number.isRequired,
  isDragging: PropTypes.bool,
  isDraggingUp: PropTypes.bool,
  isDraggingDown: PropTypes.bool,
  isOver: PropTypes.bool,
  connectDragSource: PropTypes.func,
  connectDropTarget: PropTypes.func,
  onTaggingProfileDragMove: PropTypes.func.isRequired,
  onTaggingProfileDragEnd: PropTypes.func.isRequired
};

export default DropTarget(
  TAGGING_PROFILE,
  taggingProfileDropTarget,
  collectDropTarget
)(DragSource(
  TAGGING_PROFILE,
  taggingProfileDragSource,
  collectDragSource
)(TaggingProfileDragSource));
