import PropTypes from 'prop-types';
import React from 'react';
import monitorNewItemsOptions from 'Utilities/Artist/monitorNewItemsOptions';
import translate from 'Utilities/String/translate';
import EnhancedSelectInput from './EnhancedSelectInput';

function MonitorNewItemsSelectInput(props) {
  const {
    includeNoChange,
    includeNoChangeDisabled = true,
    includeMixed,
    ...otherProps
  } = props;

  const values = [...monitorNewItemsOptions];

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

  return (
    <EnhancedSelectInput
      values={values}
      {...otherProps}
    />
  );
}

MonitorNewItemsSelectInput.propTypes = {
  includeNoChange: PropTypes.bool.isRequired,
  includeNoChangeDisabled: PropTypes.bool,
  includeMixed: PropTypes.bool.isRequired,
  onChange: PropTypes.func.isRequired
};

MonitorNewItemsSelectInput.defaultProps = {
  includeNoChange: false,
  includeMixed: false
};

export default MonitorNewItemsSelectInput;
