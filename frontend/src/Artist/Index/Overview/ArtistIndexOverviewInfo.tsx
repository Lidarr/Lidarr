import React, { useMemo } from 'react';
import { useSelector } from 'react-redux';
import Album from 'Album/Album';
import { icons } from 'Helpers/Props';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import dimensions from 'Styles/Variables/dimensions';
import formatDateTime from 'Utilities/Date/formatDateTime';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import formatBytes from 'Utilities/Number/formatBytes';
import ArtistIndexOverviewInfoRow from './ArtistIndexOverviewInfoRow';
import styles from './ArtistIndexOverviewInfo.css';

const infoRowHeight = parseInt(dimensions.artistIndexOverviewInfoRowHeight);

const rows = [
  {
    name: 'monitored',
    showProp: 'showMonitored',
    valueProp: 'monitored',
  },
  {
    name: 'qualityProfileId',
    showProp: 'showQualityProfile',
    valueProp: 'qualityProfile',
  },
  {
    name: 'lastAlbum',
    showProp: 'showLastAlbum',
    valueProp: 'lastAlbum',
  },
  {
    name: 'added',
    showProp: 'showAdded',
    valueProp: 'added',
  },
  {
    name: 'albumCount',
    showProp: 'showAlbumCount',
    valueProp: 'albumCount',
  },
  {
    name: 'path',
    showProp: 'showPath',
    valueProp: 'path',
  },
  {
    name: 'sizeOnDisk',
    showProp: 'showSizeOnDisk',
    valueProp: 'sizeOnDisk',
  },
];

function getInfoRowProps(row, props, uiSettings) {
  const { name } = row;

  if (name === 'monitored') {
    const monitoredText = props.monitored ? 'Monitored' : 'Unmonitored';

    return {
      title: monitoredText,
      iconName: props.monitored ? icons.MONITORED : icons.UNMONITORED,
      label: monitoredText,
    };
  }

  if (name === 'qualityProfileId') {
    return {
      title: 'Quality Profile',
      iconName: icons.PROFILE,
      label: props.qualityProfile.name,
    };
  }

  if (name === 'lastAlbum' && !!props.lastAlbum?.title) {
    const lastAlbum = props.lastAlbum;
    const { showRelativeDates, shortDateFormat, timeFormat } = uiSettings;

    return {
      title: `Last Album: ${lastAlbum.title}`,
      iconName: icons.CALENDAR,
      label: getRelativeDate(
        lastAlbum.releaseDate,
        shortDateFormat,
        showRelativeDates,
        {
          timeFormat,
          timeForToday: true,
        }
      ),
    };
  }

  if (name === 'added') {
    const added = props.added;
    const { showRelativeDates, shortDateFormat, longDateFormat, timeFormat } =
      uiSettings;

    return {
      title: `Added: ${formatDateTime(added, longDateFormat, timeFormat)}`,
      iconName: icons.ADD,
      label: getRelativeDate(added, shortDateFormat, showRelativeDates, {
        timeFormat,
        timeForToday: true,
      }),
    };
  }

  if (name === 'albumCount') {
    const { albumCount } = props;
    let albums = '1 album';

    if (albumCount === 0) {
      albums = 'No albums';
    } else if (albumCount > 1) {
      albums = `${albumCount} albums`;
    }

    return {
      title: 'Album Count',
      iconName: icons.CIRCLE,
      label: albums,
    };
  }

  if (name === 'path') {
    return {
      title: 'Path',
      iconName: icons.FOLDER,
      label: props.path,
    };
  }

  if (name === 'sizeOnDisk') {
    return {
      title: 'Size on Disk',
      iconName: icons.DRIVE,
      label: formatBytes(props.sizeOnDisk),
    };
  }
}

interface ArtistIndexOverviewInfoProps {
  height: number;
  showMonitored: boolean;
  showQualityProfile: boolean;
  showLastAlbum: boolean;
  showAdded: boolean;
  showAlbumCount: boolean;
  showPath: boolean;
  showSizeOnDisk: boolean;
  monitored: boolean;
  nextAlbum?: Album;
  qualityProfile: object;
  lastAlbum?: Album;
  added?: string;
  albumCount: number;
  path: string;
  sizeOnDisk?: number;
  sortKey: string;
}

function ArtistIndexOverviewInfo(props: ArtistIndexOverviewInfoProps) {
  const { height, nextAlbum } = props;

  const uiSettings = useSelector(createUISettingsSelector());

  const { shortDateFormat, showRelativeDates, longDateFormat, timeFormat } =
    uiSettings;

  let shownRows = 1;
  const maxRows = Math.floor(height / (infoRowHeight + 4));

  const rowInfo = useMemo(() => {
    return rows.map((row) => {
      const { name, showProp, valueProp } = row;

      const isVisible =
        props[valueProp] != null && (props[showProp] || props.sortKey === name);

      return {
        ...row,
        isVisible,
      };
    });
  }, [props]);

  return (
    <div className={styles.infos}>
      {!!nextAlbum?.releaseDate && (
        <ArtistIndexOverviewInfoRow
          title={formatDateTime(
            nextAlbum.releaseDate,
            longDateFormat,
            timeFormat
          )}
          iconName={icons.SCHEDULED}
          label={getRelativeDate(
            nextAlbum.releaseDate,
            shortDateFormat,
            showRelativeDates,
            {
              timeFormat,
              timeForToday: true,
            }
          )}
        />
      )}

      {rowInfo.map((row) => {
        if (!row.isVisible) {
          return null;
        }

        if (shownRows >= maxRows) {
          return null;
        }

        shownRows++;

        const infoRowProps = getInfoRowProps(row, props, uiSettings);

        return <ArtistIndexOverviewInfoRow key={row.name} {...infoRowProps} />;
      })}
    </div>
  );
}

export default ArtistIndexOverviewInfo;
