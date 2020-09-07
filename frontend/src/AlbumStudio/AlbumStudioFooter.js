import PropTypes from 'prop-types';
import React, { Component } from 'react';
import MonitorAlbumsSelectInput from 'Components/Form/MonitorAlbumsSelectInput';
import SelectInput from 'Components/Form/SelectInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import { kinds } from 'Helpers/Props';
import styles from './AlbumStudioFooter.css';

const NO_CHANGE = 'noChange';

class AlbumStudioFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      monitored: NO_CHANGE,
      monitor: NO_CHANGE
    };
  }

  componentDidUpdate(prevProps) {
    const {
      isSaving,
      saveError
    } = prevProps;

    if (prevProps.isSaving && !isSaving && !saveError) {
      this.setState({
        monitored: NO_CHANGE,
        monitor: NO_CHANGE
      });
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.setState({ [name]: value });
  }

  onUpdateSelectedPress = () => {
    const {
      monitor,
      monitored
    } = this.state;

    const changes = {};

    if (monitored !== NO_CHANGE) {
      changes.monitored = monitored === 'monitored';
    }

    if (monitor !== NO_CHANGE) {
      changes.monitor = monitor;
    }

    this.props.onUpdateSelectedPress(changes);
  }

  //
  // Render

  render() {
    const {
      selectedCount,
      isSaving
    } = this.props;

    const {
      monitored,
      monitor
    } = this.state;

    const monitoredOptions = [
      { key: NO_CHANGE, value: 'No Change', disabled: true },
      { key: 'monitored', value: 'Monitored' },
      { key: 'unmonitored', value: 'Unmonitored' }
    ];

    const noChanges = monitored === NO_CHANGE && monitor === NO_CHANGE;

    return (
      <PageContentFooter>
        <div className={styles.inputContainer}>
          <div className={styles.label}>
            Monitor Artist
          </div>

          <SelectInput
            name="monitored"
            value={monitored}
            values={monitoredOptions}
            isDisabled={!selectedCount}
            onChange={this.onInputChange}
          />
        </div>

        <div className={styles.inputContainer}>
          <div className={styles.label}>
            Monitor Albums
          </div>

          <MonitorAlbumsSelectInput
            name="monitor"
            value={monitor}
            includeNoChange={true}
            isDisabled={!selectedCount}
            onChange={this.onInputChange}
          />
        </div>

        <div>
          <div className={styles.label}>
            {selectedCount} Artist(s) Selected
          </div>

          <SpinnerButton
            className={styles.updateSelectedButton}
            kind={kinds.PRIMARY}
            isSpinning={isSaving}
            isDisabled={!selectedCount || noChanges}
            onPress={this.onUpdateSelectedPress}
          >
            Update Selected
          </SpinnerButton>
        </div>
      </PageContentFooter>
    );
  }
}

AlbumStudioFooter.propTypes = {
  selectedCount: PropTypes.number.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  onUpdateSelectedPress: PropTypes.func.isRequired
};

export default AlbumStudioFooter;
