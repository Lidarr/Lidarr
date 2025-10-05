import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputHelpText from 'Components/Form/FormInputHelpText';
import FormLabel from 'Components/Form/FormLabel';
import Measure from 'Components/Measure';
import { sizes } from 'Helpers/Props';
import DownloadProtocolItemDragPreview from './DownloadProtocolItemDragPreview';
import DownloadProtocolItemDragSource from './DownloadProtocolItemDragSource';
import styles from './DownloadProtocolItems.css';

class DownloadProtocolItems extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      height: 0
    };
  }

  //
  // Listeners

  onMeasure = ({ height }) => {
    this.setState({ height });
  };

  //
  // Render

  render() {
    const {
      dropIndex,
      dropPosition,
      items,
      errors,
      warnings,
      ...otherProps
    } = this.props;

    const {
      height
    } = this.state;

    const isDragging = dropIndex !== null;
    const isDraggingUp = isDragging && dropPosition === 'above';
    const isDraggingDown = isDragging && dropPosition === 'below';

    return (
      <FormGroup size={sizes.SMALL}>
        <FormLabel size={sizes.SMALL}>
          Download Protocols
        </FormLabel>

        <div>
          <FormInputHelpText
            text="Protocols higher in the list are more preferred. Only checked protocols are allowed"
          />

          {
            errors.map((error, index) => {
              return (
                <FormInputHelpText
                  key={index}
                  text={error.message}
                  isError={true}
                  isCheckInput={false}
                />
              );
            })
          }

          {
            warnings.map((warning, index) => {
              return (
                <FormInputHelpText
                  key={index}
                  text={warning.message}
                  isWarning={true}
                  isCheckInput={false}
                />
              );
            })
          }

          <Measure
            whitelist={['height']}
            includeMargin={false}
            onMeasure={this.onMeasure}
          >
            <div
              className={styles.qualities}
              style={{ minHeight: `${height}px` }}
            >
              <div className={styles.headerContainer}>
                <div className={styles.headerTitle}>
                  Protocol
                </div>
                <div className={styles.headerDelay}>
                  Delay (minutes)
                </div>
              </div>

              {
                items.map(({ protocol, name, allowed, delay }, index) => {
                  return (
                    <DownloadProtocolItemDragSource
                      key={protocol}
                      protocol={protocol}
                      name={name}
                      allowed={allowed}
                      delay={delay}
                      index={index}
                      isDragging={isDragging}
                      isDraggingUp={isDraggingUp}
                      isDraggingDown={isDraggingDown}
                      {...otherProps}
                    />
                  );
                })
              }

              <DownloadProtocolItemDragPreview />
            </div>
          </Measure>
        </div>
      </FormGroup>
    );
  }
}

DownloadProtocolItems.propTypes = {
  dragIndex: PropTypes.number,
  dropIndex: PropTypes.number,
  dropPosition: PropTypes.string,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  errors: PropTypes.arrayOf(PropTypes.object),
  warnings: PropTypes.arrayOf(PropTypes.object)
};

DownloadProtocolItems.defaultProps = {
  errors: [],
  warnings: []
};

export default DownloadProtocolItems;
