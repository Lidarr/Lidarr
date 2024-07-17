import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { metadataProfileNames } from 'Helpers/Props';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import sortByProp from 'Utilities/Array/sortByProp';
import translate from 'Utilities/String/translate';
import EnhancedSelectInput from './EnhancedSelectInput';

function createMapStateToProps() {
  return createSelector(
    createSortedSectionSelector('settings.metadataProfiles', sortByProp('name')),
    (state, { includeNoChange }) => includeNoChange,
    (state, { includeNoChangeDisabled }) => includeNoChangeDisabled,
    (state, { includeMixed }) => includeMixed,
    (state, { includeNone }) => includeNone,
    (metadataProfiles, includeNoChange, includeNoChangeDisabled = true, includeMixed, includeNone) => {
      const profiles = metadataProfiles.items.filter((item) => item.name !== metadataProfileNames.NONE);
      const noneProfile = metadataProfiles.items.find((item) => item.name === metadataProfileNames.NONE);

      const values = _.map(profiles, (metadataProfile) => {
        return {
          key: metadataProfile.id,
          value: metadataProfile.name
        };
      });

      if (includeNone) {
        values.push({
          key: noneProfile.id,
          value: noneProfile.name
        });
      }

      if (includeNoChange) {
        values.unshift({
          key: 'noChange',
          value: translate('NoChange'),
          isDisabled: includeNoChangeDisabled
        });
      }

      if (includeMixed) {
        values.unshift({
          key: 'mixed',
          value: '(Mixed)',
          isDisabled: true
        });
      }

      return {
        values
      };
    }
  );
}

class MetadataProfileSelectInputConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      name,
      value,
      values
    } = this.props;

    if (!value || !values.some((option) => option.key === value || parseInt(option.key) === value)) {
      const firstValue = values.find((option) => !isNaN(parseInt(option.key)));

      if (firstValue) {
        this.onChange({ name, value: firstValue.key });
      }
    }
  }

  //
  // Listeners

  onChange = ({ name, value }) => {
    this.props.onChange({ name, value: value === 'noChange' ? value : parseInt(value) });
  };

  //
  // Render

  render() {
    return (
      <EnhancedSelectInput
        {...this.props}
        onChange={this.onChange}
      />
    );
  }
}

MetadataProfileSelectInputConnector.propTypes = {
  name: PropTypes.string.isRequired,
  value: PropTypes.oneOfType([PropTypes.number, PropTypes.string]),
  values: PropTypes.arrayOf(PropTypes.object).isRequired,
  includeNoChange: PropTypes.bool.isRequired,
  includeNone: PropTypes.bool.isRequired,
  onChange: PropTypes.func.isRequired
};

MetadataProfileSelectInputConnector.defaultProps = {
  includeNoChange: false,
  includeNone: true
};

export default connect(createMapStateToProps)(MetadataProfileSelectInputConnector);
