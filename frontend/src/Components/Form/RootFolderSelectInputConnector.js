import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import RootFolderSelectInput from './RootFolderSelectInput';

const ADD_NEW_KEY = 'addNew';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.rootFolders,
    (state, { value }) => value,
    (state, { includeMissingValue }) => includeMissingValue,
    (state, { includeNoChange }) => includeNoChange,
    (state, { includeNoChangeDisabled }) => includeNoChangeDisabled,
    (rootFolders, value, includeMissingValue, includeNoChange, includeNoChangeDisabled = true) => {
      const values = rootFolders.items.map((rootFolder) => {
        return {
          key: rootFolder.path,
          value: rootFolder.path,
          name: rootFolder.name,
          freeSpace: rootFolder.freeSpace,
          isMissing: false
        };
      });

      if (includeNoChange) {
        values.unshift({
          key: 'noChange',
          value: 'No Change',
          isDisabled: includeNoChangeDisabled,
          isMissing: false
        });
      }

      if (includeMissingValue && !values.find((v) => v.key === value)) {
        values.push({
          key: value,
          value,
          isMissing: true,
          isDisabled: true
        });
      }

      if (!values.length) {
        values.push({
          key: '',
          value: '',
          isDisabled: true,
          isHidden: true
        });
      }

      values.push({
        key: ADD_NEW_KEY,
        value: 'Add a new path'
      });

      return {
        values,
        isSaving: rootFolders.isSaving,
        saveError: rootFolders.saveError
      };
    }
  );
}

class RootFolderSelectInputConnector extends Component {

  //
  // Lifecycle

  UNSAFE_componentWillMount() {
    const {
      value,
      values,
      onChange
    } = this.props;

    if (value == null && values[0].key === '') {
      onChange({ name, value: '' });
    }
  }

  componentDidMount() {
    const {
      name,
      value,
      values,
      onChange
    } = this.props;

    if (!value || !values.some((v) => v.key === value) || value === ADD_NEW_KEY) {
      const defaultValue = values[0];

      if (defaultValue.key === ADD_NEW_KEY) {
        onChange({ name, value: '' });
      } else {
        onChange({ name, value: defaultValue.key });
      }
    }
  }

  componentDidUpdate(prevProps) {
    const {
      name,
      value,
      values,
      onChange
    } = this.props;

    if (prevProps.values === values) {
      return;
    }

    if (!value && values.length && values.some((v) => !!v.key && v.key !== ADD_NEW_KEY)) {
      const defaultValue = values[0];

      if (defaultValue.key !== ADD_NEW_KEY) {
        onChange({ name, value: defaultValue.key });
      }
    }
  }

  //
  // Render

  render() {
    const {
      ...otherProps
    } = this.props;

    return (
      <RootFolderSelectInput
        {...otherProps}
        onNewRootFolderSelect={this.onNewRootFolderSelect}
      />
    );
  }
}

RootFolderSelectInputConnector.propTypes = {
  name: PropTypes.string.isRequired,
  value: PropTypes.string,
  values: PropTypes.arrayOf(PropTypes.object).isRequired,
  includeNoChange: PropTypes.bool.isRequired,
  onChange: PropTypes.func.isRequired
};

RootFolderSelectInputConnector.defaultProps = {
  includeNoChange: false
};

export default connect(createMapStateToProps)(RootFolderSelectInputConnector);
