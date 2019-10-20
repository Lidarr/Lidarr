import React, { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { getArtistStatusDetails } from 'Artist/ArtistStatus';
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
  monitored: boolean;
  status: string;
  artistType?: string;
  isSelectMode: boolean;
  isSaving: boolean;
  component?: React.ElementType;
}

function ArtistStatusCell(props: ArtistStatusCellProps) {
  const {
    className,
    artistId,
    monitored,
    status,
    artistType,
    isSelectMode,
    isSaving,
    component: Component = VirtualTableRowCell,
    ...otherProps
  } = props;

  const statusDetails = getArtistStatusDetails(status, artistType);
  const dispatch = useDispatch();

  const onMonitoredPress = useCallback(() => {
    dispatch(toggleArtistMonitored({ artistId, monitored: !monitored }));
  }, [artistId, monitored, dispatch]);

  return (
    <Component className={className} {...otherProps}>
      {isSelectMode ? (
        <MonitorToggleButton
          className={styles.statusIcon}
          monitored={monitored}
          isSaving={isSaving}
          onPress={onMonitoredPress}
        />
      ) : (
        <Icon
          className={styles.statusIcon}
          name={monitored ? icons.MONITORED : icons.UNMONITORED}
          title={
            monitored
              ? translate('ArtistIsMonitored')
              : translate('ArtistIsUnmonitored')
          }
        />
      )}

      <Icon
        className={styles.statusIcon}
        name={statusDetails.icon}
        title={`${statusDetails.title}: ${statusDetails.message}`}
      />
    </Component>
  );
}

export default ArtistStatusCell;
