import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleTracksMonitored } from 'Store/Actions/trackActions';
import ArtistDetailsTracks from './ArtistDetailsTracks';

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
      (track) => track.hasFile,
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
    const bestRatedAppearance = _.orderBy(
      appearances,
      [(track) => track.ratings?.votes || 0, (track) => track.ratings?.value || 0],
      ['desc', 'desc']
    )[0];

    return {
      ...representative,
      groupKey,
      albumTitle: album.title,
      foreignAlbumId: album.foreignAlbumId,
      monitorTrackIds: _.uniq(appearances.map((track) => track.id)),
      monitored: appearances.some((track) => track.monitored),
      isSaving: appearances.some((track) => track.isSaving),
      hasFile: appearances.some((track) => track.hasFile),
      appearanceCount: _.uniq(appearances.map((track) => track.albumId)).length,
      ratings: bestRatedAppearance.ratings || { votes: 0, value: 0 }
    };
  });

  return _.orderBy(
    songs,
    [
      (song) => song.ratings.votes,
      (song) => song.ratings.value,
      (song) => song.appearanceCount,
      (song) => song.title.toLocaleLowerCase()
    ],
    ['desc', 'desc', 'desc', 'asc']
  );
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
  toggleTracksMonitored
};

class ArtistDetailsTracksConnector extends Component {

  onTracksMonitoredChange = (trackIds, monitored) => {
    this.props.toggleTracksMonitored({ trackIds, monitored });
  };

  render() {
    return (
      <ArtistDetailsTracks
        {...this.props}
        onTracksMonitoredChange={this.onTracksMonitoredChange}
      />
    );
  }
}

ArtistDetailsTracksConnector.propTypes = {
  artistId: PropTypes.number.isRequired,
  artistMonitored: PropTypes.bool.isRequired,
  toggleTracksMonitored: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ArtistDetailsTracksConnector);
