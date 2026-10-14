import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import { fetchSearchFormatExamples, fetchSearchFormatSettings, setSearchFormatSettingsValue } from 'Store/Actions/settingsActions';
import createSettingsSectionSelector from 'Store/Selectors/createSettingsSectionSelector';
import SearchFormat from './SearchFormat';

const SECTION = 'searchFormat';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.advancedSettings,
    (state) => state.settings.searchFormatExamples,
    createSettingsSectionSelector(SECTION),
    (advancedSettings, searchFormatExamples, sectionSettings) => {
      return {
        advancedSettings,
        examples: searchFormatExamples.item,
        examplesPopulated: searchFormatExamples.isPopulated,
        ...sectionSettings
      };
    }
  );
}

const mapDispatchToProps = {
  fetchSearchFormatSettings,
  setSearchFormatSettingsValue,
  fetchSearchFormatExamples,
  clearPendingChanges
};

class SearchFormatConnector extends Component {

  //
  // Lifecycle
  //

  constructor(props, context) {
    super(props, context);

    this._searchExampleTimeout = null;
  }

  componentDidMount() {
    this.props.fetchSearchFormatSettings();
    this.props.fetchSearchFormatExamples();
  }

  componentWillUnmount() {
    this.props.clearPendingChanges({ section: 'settings.searchFormat' });
  }

  //
  // Control
  //

  _fetchSearchExamples = () => {
    this.props.fetchSearchFormatExamples();
  };

  //
  // Listeners
  //

  onInputChange = ({ name, value }) => {
    this.props.setSearchFormatSettingsValue({ name, value });

    if (this._searchExampleTimeout) {
      clearTimeout(this._searchExampleTimeout);
    }

    this._searchExampleTimeout = setTimeout(this._fetchSearchExamples, 1000);
  };

  //
  // Render
  //

  render() {
    return (
      <SearchFormat
        onInputChange={this.onInputChange}
        {...this.props}
      />
    );
  }
}

SearchFormatConnector.propTypes = {
  fetchSearchFormatSettings: PropTypes.func.isRequired,
  setSearchFormatSettingsValue: PropTypes.func.isRequired,
  fetchSearchFormatExamples: PropTypes.func.isRequired,
  clearPendingChanges: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SearchFormatConnector);
