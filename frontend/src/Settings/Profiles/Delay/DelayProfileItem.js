import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';
import { kinds } from 'Helpers/Props';

function getDelay(item) {
  if (!item.allowed) {
    return '-';
  }

  if (!item.delay) {
    return 'No Delay';
  }

  if (item.delay === 1) {
    return '1 Minute';
  }

  // TODO: use better units of time than just minutes
  return `${item.delay} Minutes`;
}

function DelayProfileItem(props) {
  const {
    name,
    allowed
  } = props;

  return (
    <Label
      kind={allowed ? kinds.INFO : kinds.DANGER}
    >
      {name}: {getDelay(props)}
    </Label>
  );
}

DelayProfileItem.propTypes = {
  name: PropTypes.string.isRequired,
  allowed: PropTypes.bool.isRequired
};

export default DelayProfileItem;
