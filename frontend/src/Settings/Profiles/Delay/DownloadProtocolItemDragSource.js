import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { DragSource, DropTarget } from 'react-dnd';
import { findDOMNode } from 'react-dom';
import { DOWNLOAD_PROTOCOL_ITEM } from 'Helpers/dragTypes';
import DownloadProtocolItem from './DownloadProtocolItem';
import styles from './DownloadProtocolItemDragSource.css';

const downloadProtocolItemDragSource = {
  beginDrag(props) {
    const {
      index,
      protocol,
      name,
      allowed,
      delay
    } = props;

    return {
      index,
      protocol,
      name,
      allowed,
      delay
    };
  },

  endDrag(props, monitor, component) {
    props.onDownloadProtocolItemDragEnd(monitor.didDrop());
  }
};

const downloadProtocolItemDropTarget = {
  hover(props, monitor, component) {
    const {
      index: dragIndex
    } = monitor.getItem();

    const dropIndex = props.index;

    // Use childNodeIndex to select the correct node to get the middle of so
    // we don't bounce between above and below causing rapid setState calls.
    const childNodeIndex = component.props.isOverCurrent && component.props.isDraggingUp ? 1 :0;
    const componentDOMNode = findDOMNode(component).children[childNodeIndex];
    const hoverBoundingRect = componentDOMNode.getBoundingClientRect();
    const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
    const clientOffset = monitor.getClientOffset();
    const hoverClientY = clientOffset.y - hoverBoundingRect.top;

    // If we're hovering over a child don't trigger on the parent
    if (!monitor.isOver({ shallow: true })) {
      return;
    }

    // Don't show targets for dropping on self
    if (dragIndex === dropIndex) {
      return;
    }

    let dropPosition = null;

    // Determine drop position based on position over target
    if (hoverClientY > hoverMiddleY) {
      dropPosition = 'below';
    } else if (hoverClientY < hoverMiddleY) {
      dropPosition = 'above';
    } else {
      return;
    }

    props.onDownloadProtocolItemDragMove({
      dragIndex,
      dropIndex,
      dropPosition
    });
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
    isOver: monitor.isOver(),
    isOverCurrent: monitor.isOver({ shallow: true })
  };
}

class DownloadProtocolItemDragSource extends Component {

  //
  // Render

  render() {
    const {
      protocol,
      name,
      allowed,
      delay,
      index,
      isDragging,
      isDraggingUp,
      isDraggingDown,
      isOverCurrent,
      connectDragSource,
      connectDropTarget,
      onDownloadProtocolItemFieldChange
    } = this.props;

    const isBefore = !isDragging && isDraggingUp && isOverCurrent;
    const isAfter = !isDragging && isDraggingDown && isOverCurrent;

    return connectDropTarget(
      <div
        className={classNames(
          styles.downloadProtocolItemDragSource,
          isBefore && styles.isDraggingUp,
          isAfter && styles.isDraggingDown
        )}
      >
        {
          isBefore &&
            <div
              className={classNames(
                styles.downloadProtocolItemPlaceholder,
                styles.downloadProtocolItemPlaceholderBefore
              )}
            />
        }

        <DownloadProtocolItem
          protocol={protocol}
          name={name}
          allowed={allowed}
          delay={delay}
          index={index}
          isDragging={isDragging}
          isOverCurrent={isOverCurrent}
          connectDragSource={connectDragSource}
          onDownloadProtocolItemFieldChange={onDownloadProtocolItemFieldChange}
        />

        {
          isAfter &&
            <div
              className={classNames(
                styles.downloadProtocolItemPlaceholder,
                styles.downloadProtocolItemPlaceholderAfter
              )}
            />
        }
      </div>
    );
  }
}

DownloadProtocolItemDragSource.propTypes = {
  protocol: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
  allowed: PropTypes.bool.isRequired,
  delay: PropTypes.number.isRequired,
  index: PropTypes.number.isRequired,
  isDragging: PropTypes.bool,
  isDraggingUp: PropTypes.bool,
  isDraggingDown: PropTypes.bool,
  isOverCurrent: PropTypes.bool,
  connectDragSource: PropTypes.func,
  connectDropTarget: PropTypes.func,
  onDownloadProtocolItemFieldChange: PropTypes.func.isRequired,
  onDownloadProtocolItemDragMove: PropTypes.func.isRequired,
  onDownloadProtocolItemDragEnd: PropTypes.func.isRequired
};

export default DropTarget(
  DOWNLOAD_PROTOCOL_ITEM,
  downloadProtocolItemDropTarget,
  collectDropTarget
)(DragSource(
  DOWNLOAD_PROTOCOL_ITEM,
  downloadProtocolItemDragSource,
  collectDragSource
)(DownloadProtocolItemDragSource));
