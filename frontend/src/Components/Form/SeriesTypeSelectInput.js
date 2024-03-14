import PropTypes from 'prop-types';
import React from 'react';
import translate from 'Utilities/String/translate';
import EnhancedSelectInput from './EnhancedSelectInput';

const artistTypeOptions = [
  { key: 'standard', value: 'Standard' },
  { key: 'daily', value: 'Daily' },
  { key: 'anime', value: 'Anime' }
];

function SeriesTypeSelectInput(props) {
  const values = [...artistTypeOptions];

  const {
    includeNoChange,
    includeNoChangeDisabled = true,
    includeMixed
  } = props;

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
      {...props}
      values={values}
    />
  );
}

SeriesTypeSelectInput.propTypes = {
  includeNoChange: PropTypes.bool.isRequired,
  includeNoChangeDisabled: PropTypes.bool,
  includeMixed: PropTypes.bool.isRequired
};

SeriesTypeSelectInput.defaultProps = {
  includeNoChange: false,
  includeMixed: false
};

export default SeriesTypeSelectInput;
