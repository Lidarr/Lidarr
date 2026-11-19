import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { hasSongFile } from 'Album/Details/songAvailability';
import TrackRowConnector from 'Album/Details/TrackRowConnector';
import Alert from 'Components/Alert';
import SelectInput from 'Components/Form/SelectInput';
import TextInput from 'Components/Form/TextInput';
import Label from 'Components/Label';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import toggleSelected from 'Utilities/Table/toggleSelected';
import { createSongSelection, filterSongs } from './songSelection';
import styles from './ArtistDetailsTracks.css';

const columns = [
  { name: 'title', label: () => translate('Title'), isVisible: true },
  { name: 'status', label: () => translate('SongLibraryStatus'), isVisible: true },
  { name: 'album', label: () => translate('Album'), isVisible: true },
  { name: 'popularity', label: () => translate('Popularity'), isVisible: true },
  { name: 'duration', label: () => translate('Duration'), isVisible: true },
  { name: 'search', label: () => translate('Search'), isVisible: true },
  { name: 'actions', label: () => translate('Actions'), isVisible: true }
];

class ArtistDetailsTracks extends Component {

  constructor(props, context) {
    super(props, context);
    this.state = {
      selectedState: createSongSelection(props.items, props.artistSongMode),
      lastToggled: null,
      query: '',
      sortKey: 'popularity',
      libraryFilter: 'all',
      isDirty: false,
      isSaving: false,
      saveError: false,
      downloadError: false,
      downloadStarted: false,
      saved: false
    };
  }

  componentDidUpdate(prevProps) {
    if (!this.state.isDirty && !this.state.isSaving &&
        (prevProps.artistSongMode !== this.props.artistSongMode || (this.props.artistSongMode && prevProps.items !== this.props.items))) {
      this.setState({ selectedState: createSongSelection(this.props.items, this.props.artistSongMode) });
    }
  }

  onQueryChange = ({ value }) => {
    this.setState({ query: value, lastToggled: null });
  };

  onSortChange = ({ value }) => {
    this.setState({ sortKey: value, lastToggled: null });
  };

  onLibraryFilterChange = ({ value }) => {
    this.setState({ libraryFilter: value, lastToggled: null });
  };

  onClearPress = () => {
    this.setState({
      selectedState: Object.fromEntries(this.props.items.map((item) => [item.groupKey, false])),
      isDirty: true,
      saved: false,
      downloadStarted: false,
      lastToggled: null
    });
  };

  onSelectAllChange = ({ value }) => {
    if (this.state.isSaving) {
      return;
    }
    const visible = filterSongs(this.props.items, this.state.query, this.state.sortKey, this.state.libraryFilter);
    this.setState((state) => ({
      selectedState: { ...state.selectedState, ...Object.fromEntries(visible.map((item) => [item.groupKey, value])) },
      isDirty: true,
      saved: false,
      downloadStarted: false
    }));
  };

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    // TableSelectCell registers on mount and unregisters on unmount. Filtering
    // unmounts rows, but must not change the user's draft wanted list.
    if (this.state.isSaving || value == null || !!this.state.selectedState[id] === value) {
      return;
    }
    const visible = filterSongs(this.props.items, this.state.query, this.state.sortKey, this.state.libraryFilter).map((item) => ({ id: item.groupKey }));
    this.setState((state) => ({
      ...toggleSelected(state, visible, id, value, shiftKey),
      isDirty: true,
      saved: false,
      downloadStarted: false
    }));
  };

  onSavePress = async(download) => {
    if (this.state.isSaving) {
      return;
    }
    this.setState({ isSaving: true, saveError: false, downloadError: false, downloadStarted: false, saved: false });
    try {
      if (download) {
        await this.props.onSongsDownload(this.state.selectedState);
        this.setState({ isSaving: false, downloadStarted: true });
      } else {
        await this.props.onSongsSelectionSave(this.state.selectedState);
        this.setState({ isSaving: false, isDirty: false, saved: true });
      }
    } catch {
      this.setState({ isSaving: false, isDirty: true, saveError: !download, downloadError: download });
    }
  };

  onSaveSelectionPress = () => this.onSavePress(false);

  onDownloadSelectedPress = () => this.onSavePress(true);

  render() {
    const { items, popularityStatus, artistSongMode } = this.props;
    const { selectedState, query, sortKey, libraryFilter, isSaving, isDirty, saveError, downloadError, downloadStarted, saved } = this.state;
    const visible = filterSongs(items, query, sortKey, libraryFilter);
    const selectedCount = items.filter((item) => selectedState[item.groupKey]).length;
    const fileCount = items.filter(hasSongFile).length;
    const allSelected = visible.length > 0 && visible.every((item) => selectedState[item.groupKey]);
    const allUnselected = visible.every((item) => !selectedState[item.groupKey]);

    return (
      <div className={styles.songs}>
        <div className={styles.header}>
          <div className={styles.left}>
            <span className={styles.title}>{translate('Songs')}</span>
            <Label kind={kinds.SUCCESS} size={sizes.LARGE}>
              {translate('SongDownloadedCount', [fileCount])}
            </Label>
            <span className={styles.missingCount}>{translate('SongMissingCount', [items.length - fileCount])}</span>
          </div>
          <div className={styles.actions}>
            <span className={styles.selectionCount}>
              {translate('SelectedSongsCountInterp', [selectedCount])}
            </span>
            <Button isDisabled={isSaving || !selectedCount} onPress={this.onClearPress}>
              {translate('ClearSelection')}
            </Button>
            {
              artistSongMode &&
                <Button isDisabled={isSaving || !isDirty} onPress={this.onSaveSelectionPress}>
                  {translate('SaveMonitoredSongs')}
                </Button>
            }
            <Button kind={kinds.PRIMARY} isDisabled={isSaving || !selectedCount}
              onPress={this.onDownloadSelectedPress}
            >
              {isSaving ? translate('Working') : translate('DownloadSelectedSongs')}
            </Button>
          </div>
        </div>
        <div className={styles.help}>
          {translate('SongSelectionHelpText')}
          {' '}
          {translate(artistSongMode ? 'SongMonitoringSelectionHelp' : 'SongDownloadKeepsAlbumMonitoring')}
        </div>
        <div className={styles.filter}>
          <label>
            {translate('SongLibraryStatus')}
            <SelectInput
              name="songLibraryFilter"
              value={libraryFilter}
              values={[
                { key: 'all', value: translate('AllSongs') },
                { key: 'missing', value: translate('Missing') },
                { key: 'downloaded', value: translate('Downloaded') }
              ]}
              onChange={this.onLibraryFilterChange}
            />
          </label>
          <label className={styles.searchFilter}>
            {translate('FindSongs')}
            <TextInput
              name="songFilter"
              value={query}
              placeholder={translate('FilterSongsPlaceholder')}
              onChange={this.onQueryChange}
            />
          </label>
          <label>
            {translate('SortSongs')}
            <SelectInput
              name="songSort"
              value={sortKey}
              values={[
                { key: 'popularity', value: translate('MostPopular') },
                { key: 'title', value: translate('Title') },
                { key: 'album', value: translate('Album') }
              ]}
              onChange={this.onSortChange}
            />
          </label>
        </div>
        <div className={styles.help}>
          {translate({ loading: 'SongPopularityLoading', available: 'SongPopularitySourceHelp' }[popularityStatus] || 'SongPopularityUnavailable')}
        </div>
        {
          saveError &&
            <Alert kind={kinds.DANGER} role="alert">{translate('SongSelectionSaveError')}</Alert>
        }
        {
          downloadError &&
            <Alert kind={kinds.DANGER} role="alert">{translate('SongDownloadError')}</Alert>
        }
        {
          downloadStarted &&
            <Alert kind={kinds.SUCCESS} role="status">
              {translate('SongDownloadSearchStarted')}
              {' '}<Link to="/activity/queue">{translate('Queue')}</Link>
            </Alert>
        }
        {
          saved &&
            <Alert kind={kinds.SUCCESS} role="status">{translate('SongSelectionSaved')}</Alert>
        }
        <div className={styles.tracks}>
          <Table
            selectAll={true}
            allSelected={allSelected}
            allUnselected={allUnselected}
            columns={columns}
            onSelectAllChange={this.onSelectAllChange}
          >
            <TableBody>
              {
                visible.map((item) => (
                  <TrackRowConnector
                    key={item.groupKey}
                    {...item}
                    columns={columns}
                    isSelectMode={true}
                    isRowSelectable={true}
                    isSelectionDisabled={isSaving}
                    isSelected={!!selectedState[item.groupKey]}
                    selectionId={item.groupKey}
                    onSelectedChange={this.onSelectedChange}
                  />
                ))
              }
            </TableBody>
          </Table>
          {
            !visible.length &&
              <div className={styles.help}>{translate('NoSongsMatchFilter')}</div>
          }
        </div>
      </div>
    );
  }
}

ArtistDetailsTracks.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  artistSongMode: PropTypes.bool.isRequired,
  popularityStatus: PropTypes.string,
  onSongsSelectionSave: PropTypes.func.isRequired,
  onSongsDownload: PropTypes.func.isRequired
};

export default ArtistDetailsTracks;
