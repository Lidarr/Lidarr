import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './ArtistStatusCell.css';

function ArtistStatusCell(props) {
  const {
    className,
    artistType,
    monitored,
    status,
    isSaving,
    onMonitoredPress,
    component: Component,
    ...otherProps
  } = props;

  const endedString = artistType === 'Person' ? 'Deceased' : 'Ended';

  return (
    <Component
      className={className}
      {...otherProps}
    >
      <MonitorToggleButton
        className={styles.monitorToggle}
        monitored={monitored}
        size={14}
        isSaving={isSaving}
        onPress={onMonitoredPress}
      />

      <Icon
        className={styles.statusIcon}
        name={status === 'ended' ? icons.ARTIST_ENDED : icons.ARTIST_CONTINUING}
        title={status === 'ended' ? endedString : translate('StatusEndedContinuing')}
      />
    </Component>
  );
}

ArtistStatusCell.propTypes = {
  className: PropTypes.string.isRequired,
  artistType: PropTypes.string,
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  isSaving: PropTypes.bool.isRequired,
  onMonitoredPress: PropTypes.func.isRequired,
  component: PropTypes.elementType
};

ArtistStatusCell.defaultProps = {
  className: styles.status,
  component: VirtualTableRowCell
};

export default ArtistStatusCell;
