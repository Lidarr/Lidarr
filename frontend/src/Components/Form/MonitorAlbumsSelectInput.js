import PropTypes from 'prop-types';
import React from 'react';
import monitorOptions from 'Utilities/Artist/monitorOptions';
import translate from 'Utilities/String/translate';
import SelectInput from './SelectInput';

function MonitorAlbumsSelectInput(props) {
  const {
    includeNoChange,
    includeNoChangeDisabled = true,
    includeMixed,
    ...otherProps
  } = props;

  const values = [...monitorOptions];

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
    <SelectInput
      values={values}
      {...otherProps}
    />
  );
}

MonitorAlbumsSelectInput.propTypes = {
  includeNoChange: PropTypes.bool.isRequired,
  includeNoChangeDisabled: PropTypes.bool,
  includeMixed: PropTypes.bool.isRequired,
  onChange: PropTypes.func.isRequired
};

MonitorAlbumsSelectInput.defaultProps = {
  includeNoChange: false,
  includeMixed: false
};

export default MonitorAlbumsSelectInput;
