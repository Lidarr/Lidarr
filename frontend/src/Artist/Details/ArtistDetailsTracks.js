import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TrackRowConnector from 'Album/Details/TrackRowConnector';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './ArtistDetailsTracks.css';

const columns = [
  {
    name: 'monitored',
    label: () => translate('Monitored'),
    isVisible: true
  },
  {
    name: 'title',
    label: () => translate('Title'),
    isVisible: true
  },
  {
    name: 'album',
    label: () => translate('Album'),
    isVisible: true
  },
  {
    name: 'duration',
    label: () => translate('Duration'),
    isVisible: true
  },
  {
    name: 'popularity',
    label: () => translate('Appearances'),
    isVisible: true
  },
  {
    name: 'status',
    label: () => translate('Status'),
    isVisible: true
  },
  {
    name: 'search',
    label: () => translate('Search'),
    isVisible: true
  },
  {
    name: 'actions',
    label: () => translate('Actions'),
    isVisible: true
  }
];

function getCountKind(artistMonitored, fileCount, totalCount) {
  if (totalCount > 0 && fileCount === totalCount) {
    return kinds.SUCCESS;
  }

  if (!artistMonitored) {
    return kinds.WARNING;
  }

  return kinds.DANGER;
}

class ArtistDetailsTracks extends Component {

  constructor(props, context) {
    super(props, context);

    this.state = {
      isExpanded: false
    };
  }

  onExpandPress = () => {
    this.setState((state) => ({ isExpanded: !state.isExpanded }));
  };

  onMonitorAllPress = (monitored) => {
    const trackIds = _.uniq(_.flatMap(this.props.items, 'monitorTrackIds'));
    this.props.onTracksMonitoredChange(trackIds, monitored);
  };

  render() {
    const {
      artistMonitored,
      items
    } = this.props;

    const {
      isExpanded
    } = this.state;

    const monitoredCount = items.filter((track) => track.monitored).length;
    const fileCount = items.filter((track) => track.hasFile).length;
    const allTracksMonitored = items.length > 0 && monitoredCount === items.length;
    const isSaving = items.some((track) => track.isSaving);
    const popularFirstLabel = translate('PopularFirst');

    return (
      <div className={styles.songs}>
        <div className={styles.header}>
          <div className={styles.left}>
            <MonitorToggleButton
              monitored={allTracksMonitored}
              isSaving={isSaving}
              size={24}
              onPress={this.onMonitorAllPress}
            />

            <span className={styles.title}>
              {translate('Songs')}
            </span>

            <Label
              title={translate('TotalSongCountSongsTotalSongFileCountSongsWithFilesInterp', [items.length, fileCount])}
              kind={getCountKind(artistMonitored, fileCount, items.length)}
              size={sizes.LARGE}
            >
              <span>{fileCount} / {items.length}</span>
            </Label>

            <span
              className={styles.rankingHelp}
              title={translate('ArtistSongsPopularityHelpText')}
            >
              {popularFirstLabel === 'PopularFirst' ? 'Popular first' : popularFirstLabel}
            </span>
          </div>

          <Link
            className={styles.expandButton}
            onPress={this.onExpandPress}
          >
            <Icon
              className={styles.expandButtonIcon}
              name={isExpanded ? icons.COLLAPSE : icons.EXPAND}
              title={isExpanded ? translate('IsExpandedHideTracks') : translate('IsExpandedShowTracks')}
              size={24}
            />
          </Link>
        </div>

        {
          isExpanded &&
            <div className={styles.tracks}>
              <Table columns={columns}>
                <TableBody>
                  {
                    items.map((item) => {
                      return (
                        <TrackRowConnector
                          key={item.groupKey}
                          columns={columns}
                          {...item}
                        />
                      );
                    })
                  }
                </TableBody>
              </Table>

              <div className={styles.collapseButtonContainer}>
                <IconButton
                  name={icons.COLLAPSE}
                  size={20}
                  title={translate('HideTracks')}
                  onPress={this.onExpandPress}
                />
              </div>
            </div>
        }
      </div>
    );
  }
}

ArtistDetailsTracks.propTypes = {
  artistMonitored: PropTypes.bool.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  onTracksMonitoredChange: PropTypes.func.isRequired
};

export default ArtistDetailsTracks;
