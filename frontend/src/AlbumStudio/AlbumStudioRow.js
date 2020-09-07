import PropTypes from 'prop-types';
import React, { Component } from 'react';
import ArtistNameLink from 'Artist/ArtistNameLink';
import ArtistStatusCell from 'Artist/Index/Table/ArtistStatusCell';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import AlbumStudioAlbum from './AlbumStudioAlbum';
import styles from './AlbumStudioRow.css';

class AlbumStudioRow extends Component {

  //
  // Render

  render() {
    const {
      artistId,
      status,
      foreignArtistId,
      artistName,
      artistType,
      monitored,
      albums,
      isSaving,
      isSelected,
      onSelectedChange,
      onArtistMonitoredPress,
      onAlbumMonitoredPress
    } = this.props;

    return (
      <>
        <VirtualTableSelectCell
          className={styles.selectCell}
          id={artistId}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
          isDisabled={false}
        />

        <ArtistStatusCell
          className={styles.status}
          artistType={artistType}
          monitored={monitored}
          status={status}
          isSaving={isSaving}
          onMonitoredPress={onArtistMonitoredPress}
          component={VirtualTableRowCell}
        />

        <VirtualTableRowCell className={styles.title}>
          <ArtistNameLink
            foreignArtistId={foreignArtistId}
            artistName={artistName}
          />
        </VirtualTableRowCell>

        <VirtualTableRowCell className={styles.albums}>
          {
            albums.map((album) => {
              return (
                <AlbumStudioAlbum
                  key={album.id}
                  {...album}
                  onAlbumMonitoredPress={onAlbumMonitoredPress}
                />
              );
            })
          }
        </VirtualTableRowCell>
      </>
    );
  }
}

AlbumStudioRow.propTypes = {
  artistId: PropTypes.number.isRequired,
  status: PropTypes.string.isRequired,
  foreignArtistId: PropTypes.string.isRequired,
  artistName: PropTypes.string.isRequired,
  artistType: PropTypes.string,
  monitored: PropTypes.bool.isRequired,
  albums: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  onArtistMonitoredPress: PropTypes.func.isRequired,
  onAlbumMonitoredPress: PropTypes.func.isRequired
};

AlbumStudioRow.defaultProps = {
  isSaving: false
};

export default AlbumStudioRow;
