import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { hasSongFile } from 'Album/Details/songAvailability';
import * as commandNames from 'Commands/commandNames';
import { toggleArtistMonitored } from 'Store/Actions/artistActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { toggleTracksMonitored } from 'Store/Actions/trackActions';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import ArtistDetailsTracks from './ArtistDetailsTracks';
import { downloadSongSelection, saveSongSelection } from './songSelection';

const albumTypePriority = {
  Album: 0,
  EP: 1,
  Single: 2,
  Broadcast: 3,
  Other: 4
};

function getGroupKey(track) {
  if (track.foreignRecordingId) {
    return track.foreignRecordingId;
  }

  return `${track.title.toLocaleLowerCase()}-${Math.round(track.duration / 1000)}`;
}

function getAlbumPriority(album) {
  if (album.secondaryTypes?.includes('Compilation')) {
    return 10;
  }

  return albumTypePriority[album.albumType] ?? 5;
}

function getRepresentative(tracks, albumsById) {
  return _.orderBy(
    tracks,
    [
      hasSongFile,
      (track) => track.monitored,
      (track) => getAlbumPriority(albumsById[track.albumId]),
      (track) => albumsById[track.albumId].releaseDate || '9999-12-31',
      (track) => albumsById[track.albumId].title.toLocaleLowerCase()
    ],
    ['desc', 'desc', 'asc', 'asc', 'asc']
  )[0];
}

export function createArtistSongs(tracks, albums) {
  const albumsById = _.keyBy(albums, 'id');
  const tracksWithAlbums = tracks.filter((track) => albumsById[track.albumId]);
  const groupedTracks = _.groupBy(tracksWithAlbums, getGroupKey);

  const songs = Object.entries(groupedTracks).map(([groupKey, appearances]) => {
    const representative = getRepresentative(appearances, albumsById);
    const album = albumsById[representative.albumId];

    return {
      ...representative,
      groupKey,
      albumTitle: album.title,
      foreignAlbumId: album.foreignAlbumId,
      monitorTrackIds: _.uniq(appearances.map((track) => track.id)),
      monitored: appearances.some((track) => track.monitored),
      isSaving: appearances.some((track) => track.isSaving),
      hasFile: appearances.some(hasSongFile),
      appearanceCount: _.uniq(appearances.map((track) => track.albumId)).length
    };
  });

  return _.sortBy(songs, (song) => song.title.toLocaleLowerCase());
}

function createMapStateToProps() {
  return createSelector(
    (state, { artistId }) => artistId,
    (state) => state.tracks.items,
    (state) => state.albums.items,
    (artistId, tracks, albums) => {
      const artistTracks = tracks.filter((track) => track.artistId === artistId);

      return {
        items: createArtistSongs(artistTracks, albums)
      };
    }
  );
}

const mapDispatchToProps = {
  executeCommand,
  toggleArtistMonitored,
  toggleTracksMonitored
};

class ArtistDetailsTracksConnector extends Component {

  state = { popularityStatus: 'loading', popularityByRecordingId: {} };

  componentDidMount() {
    this.fetchPopularity();
  }

  componentDidUpdate(prevProps) {
    if (prevProps.artistId !== this.props.artistId) {
      this.fetchPopularity();
    }
  }

  componentWillUnmount() {
    this.abortPopularity?.();
  }

  fetchPopularity = () => {
    this.abortPopularity?.();
    const artistId = this.props.artistId;
    this.setState({ popularityStatus: 'loading', popularityByRecordingId: {} });
    const { request, abortRequest } = createAjaxRequest({ url: '/track/popularity', data: { artistId }, dataType: 'json' });
    this.abortPopularity = abortRequest;
    request.done((result) => {
      if (this.props.artistId === artistId) {
        this.setState({ popularityStatus: result.status, popularityByRecordingId: _.keyBy(result.tracks, 'foreignRecordingId') });
      }
    });
    request.fail((error) => {
      if (!error.aborted && this.props.artistId === artistId) {
        this.setState({ popularityStatus: 'unavailable' });
      }
    });
  };

  onSongsDownload = (selectedState) => {
    return downloadSongSelection({
      items: this.props.items,
      selectedState,
      search: (trackIds) => this.props.executeCommand({ name: commandNames.TRACK_SEARCH, trackIds })
    });
  };

  onSongsSelectionSave = (selectedState) => {
    return saveSongSelection({
      items: this.props.items,
      selectedState,
      monitor: (trackIds, monitored) => this.props.toggleTracksMonitored({ trackIds, monitored }),
      enableArtist: () => (this.props.artistMonitored ? undefined : this.props.toggleArtistMonitored({ artistId: this.props.artistId, monitored: true }))
    });
  };

  render() {
    return (
      <ArtistDetailsTracks
        {...this.props}
        items={this.props.items.map((item) => ({ ...item, popularity: this.state.popularityByRecordingId[item.groupKey] }))}
        popularityStatus={this.state.popularityStatus}
        onSongsSelectionSave={this.onSongsSelectionSave}
        onSongsDownload={this.onSongsDownload}
      />
    );
  }
}

ArtistDetailsTracksConnector.propTypes = {
  artistId: PropTypes.number.isRequired,
  artistName: PropTypes.string.isRequired,
  artistMonitored: PropTypes.bool.isRequired,
  artistSongMode: PropTypes.bool.isRequired,
  executeCommand: PropTypes.func.isRequired,
  toggleArtistMonitored: PropTypes.func.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  toggleTracksMonitored: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ArtistDetailsTracksConnector);
