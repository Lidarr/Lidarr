import { IconDefinition } from '@fortawesome/free-regular-svg-icons';
import React, { useMemo } from 'react';
import { useSelector } from 'react-redux';
import Album from 'Album/Album';
import { icons } from 'Helpers/Props';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import dimensions from 'Styles/Variables/dimensions';
import QualityProfile from 'typings/QualityProfile';
import { UiSettings } from 'typings/UiSettings';
import formatDateTime from 'Utilities/Date/formatDateTime';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import ArtistIndexOverviewInfoRow from './ArtistIndexOverviewInfoRow';
import styles from './ArtistIndexOverviewInfo.css';

interface RowProps {
  name: string;
  showProp: string;
  valueProp: string;
}

interface RowInfoProps {
  title: string;
  iconName: IconDefinition;
  label: string;
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
  qualityProfile?: QualityProfile;
  lastAlbum?: Album;
  added?: string;
  albumCount: number;
  path: string;
  sizeOnDisk?: number;
  sortKey: string;
}

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

function getInfoRowProps(
  row: RowProps,
  props: ArtistIndexOverviewInfoProps,
  uiSettings: UiSettings
): RowInfoProps | null {
  const { name } = row;

  if (name === 'monitored') {
    const monitoredText = props.monitored
      ? translate('Monitored')
      : translate('Unmonitored');

    return {
      title: monitoredText,
      iconName: props.monitored ? icons.MONITORED : icons.UNMONITORED,
      label: monitoredText,
    };
  }

  if (name === 'qualityProfileId' && !!props.qualityProfile?.name) {
    return {
      title: translate('QualityProfile'),
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
      label:
        getRelativeDate(
          lastAlbum.releaseDate,
          shortDateFormat,
          showRelativeDates,
          {
            timeFormat,
            timeForToday: true,
          }
        ) ?? '',
    };
  }

  if (name === 'added') {
    const added = props.added;
    const { showRelativeDates, shortDateFormat, longDateFormat, timeFormat } =
      uiSettings;

    return {
      title: `Added: ${formatDateTime(added, longDateFormat, timeFormat)}`,
      iconName: icons.ADD,
      label:
        getRelativeDate(added, shortDateFormat, showRelativeDates, {
          timeFormat,
          timeForToday: true,
        }) ?? '',
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
      title: translate('AlbumCount'),
      iconName: icons.CIRCLE,
      label: albums,
    };
  }

  if (name === 'path') {
    return {
      title: translate('Path'),
      iconName: icons.FOLDER,
      label: props.path,
    };
  }

  if (name === 'sizeOnDisk') {
    return {
      title: translate('SizeOnDisk'),
      iconName: icons.DRIVE,
      label: formatBytes(props.sizeOnDisk),
    };
  }

  return null;
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
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore ts(7053)
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

        if (infoRowProps == null) {
          return null;
        }

        return <ArtistIndexOverviewInfoRow key={row.name} {...infoRowProps} />;
      })}
    </div>
  );
}

export default ArtistIndexOverviewInfo;
