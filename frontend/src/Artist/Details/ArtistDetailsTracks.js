import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TrackRowConnector from 'Album/Details/TrackRowConnector';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import Button from 'Components/Link/Button';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import areAllSelected from 'Utilities/Table/areAllSelected';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import EditSelectedSongsModal from './EditSelectedSongsModal';
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

function getSelectionState(items) {
  const selectedState = _.zipObject(
    items.map((item) => item.groupKey),
    items.map(() => false)
  );

  return {
    ...areAllSelected(selectedState),
    lastToggled: null,
    selectedState
  };
}

function translateWithFallback(key, fallback, args) {
  const translated = translate(key, args);
  return translated === key ? fallback : translated;
}

class ArtistDetailsTracks extends Component {

  constructor(props, context) {
    super(props, context);

    this.state = {
      isExpanded: false,
      isSelectMode: false,
      isEditSelectedSongsModalOpen: false,
      ...getSelectionState(props.items)
    };
  }

  componentDidUpdate(prevProps) {
    if (!this.state.isSelectMode && prevProps.items !== this.props.items) {
      this.setState(getSelectionState(this.props.items));
    }
  }

  onExpandPress = () => {
    this.setState((state) => ({ isExpanded: !state.isExpanded }));
  };

  onMonitorAllPress = (monitored) => {
    const trackIds = _.uniq(_.flatMap(this.props.items, 'monitorTrackIds'));
    this.props.onTracksMonitoredChange(trackIds, monitored);
  };

  onSelectSongsPress = () => {
    this.setState({
      isExpanded: true,
      isSelectMode: true,
      ...getSelectionState(this.props.items)
    });
  };

  onCancelSelectionPress = () => {
    this.setState({
      isSelectMode: false,
      ...getSelectionState(this.props.items)
    });
  };

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  };

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    const selectableItems = this.props.items.map((item) => ({ id: item.groupKey }));

    this.setState((state) => {
      return toggleSelected(state, selectableItems, id, value, shiftKey);
    });
  };

  onEditSelectedSongsPress = () => {
    this.setState({ isEditSelectedSongsModalOpen: true });
  };

  onEditSelectedSongsModalClose = () => {
    this.setState({ isEditSelectedSongsModalOpen: false });
  };

  onEditSelectedSongsSave = (monitored) => {
    const {
      items,
      onSongsSelectionSave
    } = this.props;

    const {
      selectedState
    } = this.state;

    const trackIds = _.uniq(_.flatMap(
      items.filter((item) => selectedState[item.groupKey]),
      'monitorTrackIds'
    ));

    onSongsSelectionSave(trackIds, monitored);
    this.setState({
      isEditSelectedSongsModalOpen: false,
      isSelectMode: false,
      ...getSelectionState(items)
    });
  };

  render() {
    const {
      artistName,
      artistMonitored,
      items
    } = this.props;

    const {
      allSelected,
      allUnselected,
      isEditSelectedSongsModalOpen,
      isExpanded,
      isSelectMode,
      selectedState
    } = this.state;

    const monitoredCount = items.filter((track) => track.monitored).length;
    const fileCount = items.filter((track) => track.hasFile).length;
    const allTracksMonitored = items.length > 0 && monitoredCount === items.length;
    const isSaving = items.some((track) => track.isSaving);
    const popularFirstLabel = translate('PopularFirst');
    const selectedCount = Object.values(selectedState).filter(Boolean).length;
    const tableColumns = isSelectMode ?
      columns.filter((column) => column.name !== 'monitored') :
      columns;

    return (
      <div className={styles.songs}>
        <div className={styles.header}>
          <div className={styles.left}>
            {
              isSelectMode ?
                null :
                <MonitorToggleButton
                  monitored={allTracksMonitored}
                  isSaving={isSaving}
                  size={24}
                  onPress={this.onMonitorAllPress}
                />
            }

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

          <div className={styles.actions}>
            {
              isSelectMode ?
                <>
                  <span className={styles.selectionCount}>
                    {translateWithFallback('SelectedSongsCountInterp', `${selectedCount} selected`, [selectedCount])}
                  </span>

                  <Button
                    size={sizes.SMALL}
                    onPress={this.onCancelSelectionPress}
                  >
                    {translate('Cancel')}
                  </Button>

                  <Button
                    size={sizes.SMALL}
                    isDisabled={!selectedCount || isSaving}
                    onPress={this.onEditSelectedSongsPress}
                  >
                    {translate('Edit')}
                  </Button>
                </> :
                <Button
                  size={sizes.SMALL}
                  onPress={this.onSelectSongsPress}
                >
                  {translateWithFallback('SelectSongs', 'Select songs')}
                </Button>
            }
          </div>
        </div>

        {
          isExpanded &&
            <div className={styles.tracks}>
              <Table
                selectAll={isSelectMode}
                allSelected={allSelected}
                allUnselected={allUnselected}
                columns={tableColumns}
                onSelectAllChange={this.onSelectAllChange}
              >
                <TableBody>
                  {
                    items.map((item) => {
                      return (
                        <TrackRowConnector
                          key={item.groupKey}
                          columns={tableColumns}
                          isSelectMode={isSelectMode}
                          isSelected={!!selectedState[item.groupKey]}
                          selectionId={item.groupKey}
                          {...item}
                          onSelectedChange={this.onSelectedChange}
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

        <EditSelectedSongsModal
          isOpen={isEditSelectedSongsModalOpen}
          artistName={artistName}
          selectedCount={selectedCount}
          onSavePress={this.onEditSelectedSongsSave}
          onModalClose={this.onEditSelectedSongsModalClose}
        />
      </div>
    );
  }
}

ArtistDetailsTracks.propTypes = {
  artistName: PropTypes.string.isRequired,
  artistMonitored: PropTypes.bool.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  onTracksMonitoredChange: PropTypes.func.isRequired,
  onSongsSelectionSave: PropTypes.func.isRequired
};

export default ArtistDetailsTracks;
