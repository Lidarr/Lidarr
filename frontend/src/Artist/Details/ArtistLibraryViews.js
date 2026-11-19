import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import translate from 'Utilities/String/translate';
import ArtistDetailsTracksConnector from './ArtistDetailsTracksConnector';
import { getArtistLibraryViewLink } from './artistLibraryView';
import styles from './ArtistLibraryViews.css';

class ArtistLibraryViews extends Component {

  constructor(props) {
    super(props);
    this.state = { hasOpenedSongs: props.view === 'songs' };
  }

  componentDidUpdate() {
    if (this.props.view === 'songs' && !this.state.hasOpenedSongs) {
      this.setState({ hasOpenedSongs: true });
    }
  }

  render() {
    const { artistId, foreignArtistId, artistName, artistMonitored, artistSongMode, view,
      isPopulated, hasTracks, isRefreshing, onRefreshPress, children } = this.props;
    const showSongs = view === 'songs';

    return (
      <div>
        <nav className={styles.views} aria-label={translate('ArtistLibraryViews')}>
          {['albums', 'songs'].map((item) => (
            <Link
              key={item}
              to={getArtistLibraryViewLink(foreignArtistId, item)}
              className={classNames(styles.view, view === item && styles.activeView)}
              aria-current={view === item ? 'page' : undefined}
            >
              {translate(item === 'songs' ? 'Songs' : 'Albums')}
            </Link>
          ))}
        </nav>

        <div hidden={showSongs} aria-label={translate('Albums')}>
          {children}
        </div>

        {
          isPopulated && (showSongs || this.state.hasOpenedSongs) &&
            <div hidden={!showSongs} aria-label={translate('Songs')}>
              {
                hasTracks ?
                  <ArtistDetailsTracksConnector
                    artistId={artistId}
                    artistName={artistName}
                    artistMonitored={artistMonitored}
                    artistSongMode={artistSongMode}
                  /> :
                  <div className={styles.empty}>
                    <p>{translate('NoSongsAvailable')}</p>
                    <Button onPress={onRefreshPress} isDisabled={isRefreshing}>{translate('RefreshScan')}</Button>
                  </div>
              }
            </div>
        }
      </div>
    );
  }
}

ArtistLibraryViews.propTypes = {
  artistId: PropTypes.number.isRequired,
  foreignArtistId: PropTypes.string.isRequired,
  artistName: PropTypes.string.isRequired,
  artistMonitored: PropTypes.bool.isRequired,
  artistSongMode: PropTypes.bool.isRequired,
  view: PropTypes.oneOf(['albums', 'songs']).isRequired,
  isPopulated: PropTypes.bool.isRequired,
  hasTracks: PropTypes.bool.isRequired,
  isRefreshing: PropTypes.bool.isRequired,
  onRefreshPress: PropTypes.func.isRequired,
  children: PropTypes.node
};

export default ArtistLibraryViews;
