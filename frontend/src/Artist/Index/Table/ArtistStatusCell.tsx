import React, { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Icon from 'Components/Icon';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons } from 'Helpers/Props';
import { toggleArtistMonitored } from 'Store/Actions/artistActions';
import translate from 'Utilities/String/translate';
import styles from './ArtistStatusCell.css';

interface ArtistStatusCellProps {
  className: string;
  artistId: number;
  artistType?: string;
  monitored: boolean;
  status: string;
  isSaving: boolean;
  component?: React.ElementType;
}

function ArtistStatusCell(props: ArtistStatusCellProps) {
  const {
    className,
    artistId,
    artistType,
    monitored,
    status,
    isSaving,
    component: Component = VirtualTableRowCell,
    ...otherProps
  } = props;

  const endedString = artistType === 'Person' ? 'Deceased' : 'Inactive';
  const dispatch = useDispatch();

  const onMonitoredPress = useCallback(() => {
    dispatch(toggleArtistMonitored({ artistId, monitored: !monitored }));
  }, [artistId, monitored, dispatch]);

  return (
    <Component className={className} {...otherProps}>
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
        title={
          status === 'ended' ? endedString : translate('StatusEndedContinuing')
        }
      />
    </Component>
  );
}

export default ArtistStatusCell;
