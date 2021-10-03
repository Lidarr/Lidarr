import PropTypes from 'prop-types';
import React from 'react';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';

function QualityDefinitionLimits(props) {
  const {
    bytes,
    message
  } = props;

  if (!bytes) {
    return <div>{message}</div>;
  }

  const twenty = formatBytes(bytes * 20 * 60);
  const fourtyFive = formatBytes(bytes * 45 * 60);
  const sixty = formatBytes(bytes * 60 * 60);

  return (
    <div>
      <div>
        {translate('20MinutesTwenty', [twenty])}
      </div>
      <div>
        {translate('45MinutesFourtyFive', [fourtyFive])}
      </div>
      <div>
        {translate('60MinutesSixty', [sixty])}
      </div>
    </div>
  );
}

QualityDefinitionLimits.propTypes = {
  bytes: PropTypes.number,
  message: PropTypes.string.isRequired
};

export default QualityDefinitionLimits;
