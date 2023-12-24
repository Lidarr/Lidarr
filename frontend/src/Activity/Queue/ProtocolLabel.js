import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';
import styles from './ProtocolLabel.css';

function ProtocolLabel({ protocol }) {
  const strippedName = protocol.replace('DownloadProtocol', '').toLowerCase();
  const protocolName = strippedName === 'usenet' ? 'nzb' : strippedName;

  return (
    <Label className={styles[protocol]}>
      {protocolName}
    </Label>
  );
}

ProtocolLabel.propTypes = {
  protocol: PropTypes.string.isRequired
};

export default ProtocolLabel;
