import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { icons } from 'Helpers/Props';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import TextInput from 'Components/Form/TextInput';
import PageContent from 'Components/Page/PageContent';
import PageContentBodyConnector from 'Components/Page/PageContentBodyConnector';
import AddNewArtistSearchResultConnector from './Artist/AddNewArtistSearchResultConnector';
import AddNewAlbumSearchResultConnector from './Album/AddNewAlbumSearchResultConnector';
import styles from './AddNewItem.css';

class AddNewItem extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      term: props.term || '',
      isFetching: false
    };
  }

  componentDidMount() {
    const term = this.state.term;

    if (term) {
      this.props.onSearchChange(term);
    }
  }

  componentDidUpdate(prevProps) {
    const {
      term,
      isFetching
    } = this.props;

    if (term && term !== prevProps.term) {
      this.setState({
        term,
        isFetching: true
      });
      this.props.onSearchChange(term);
    } else if (isFetching !== prevProps.isFetching) {
      this.setState({
        isFetching
      });
    }
  }

  //
  // Listeners

  onSearchInputChange = ({ value }) => {
    const hasValue = !!value.trim();

    this.setState({ term: value, isFetching: hasValue }, () => {
      if (hasValue) {
        this.props.onSearchChange(value);
      } else {
        this.props.onClearSearch();
      }
    });
  }

  onClearSearchPress = () => {
    this.setState({ term: '' });
    this.props.onClearSearch();
  }

  //
  // Render

  render() {
    const {
      error,
      items
    } = this.props;

    const term = this.state.term;
    const isFetching = this.state.isFetching;

    return (
      <PageContent title="Add New Item">
        <PageContentBodyConnector>
          <div className={styles.searchContainer}>
            <div className={styles.searchIconContainer}>
              <Icon
                name={icons.SEARCH}
                size={20}
              />
            </div>

            <TextInput
              className={styles.searchInput}
              name="searchBox"
              value={term}
              placeholder="eg. Breaking Benjamin, lidarr:854a1807-025b-42a8-ba8c-2a39717f1d25"
              autoFocus={true}
              onChange={this.onSearchInputChange}
            />

            <Button
              className={styles.clearLookupButton}
              onPress={this.onClearSearchPress}
            >
              <Icon
                name={icons.REMOVE}
                size={20}
              />
            </Button>
          </div>

          {
            isFetching &&
              <LoadingIndicator />
          }

          {
            !isFetching && !!error &&
              <div>Failed to load search results, please try again.</div>
          }

          {
            !isFetching && !error && !!items.length &&
              <div className={styles.searchResults}>
                {
                  items.map((item) => {
                    if (item.artist) {
                      const artist = item.artist;
                      return (
                        <AddNewArtistSearchResultConnector
                          key={item.id}
                          {...artist}
                        />
                      );
                    } else if (item.album) {
                      const album = item.album;
                      return (
                        <AddNewAlbumSearchResultConnector
                          key={item.id}
                          isExistingAlbum={'id' in album && album.id !== 0}
                          isExistingArtist={'id' in album.artist && album.artist.id !== 0}
                          {...album}
                        />
                      );
                    }
                    return null;
                  })
                }
              </div>
          }

          {
            !isFetching && !error && !items.length && !!term &&
              <div className={styles.message}>
                <div className={styles.noResults}>Couldn't find any results for '{term}'</div>
                <div>
                  You can also search using the
                  <Link to="https://musicbrainz.org/search"> MusicBrainz ID </Link>
                  of an artist e.g. lidarr:cc197bad-dc9c-440d-a5b5-d52ba2e14234
                </div>
              </div>
          }

          {
            !term &&
              <div className={styles.message}>
                <div className={styles.helpText}>It's easy to add a new artist, just start typing the name of the artist you want to add.</div>
                <div>
                  You can also search using the
                  <Link to="https://musicbrainz.org/search"> MusicBrainz ID </Link>
                  of an artist e.g. lidarr:cc197bad-dc9c-440d-a5b5-d52ba2e14234
                </div>
              </div>
          }

          <div />
        </PageContentBodyConnector>
      </PageContent>
    );
  }
}

AddNewItem.propTypes = {
  term: PropTypes.string,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isAdding: PropTypes.bool.isRequired,
  addError: PropTypes.object,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  onSearchChange: PropTypes.func.isRequired,
  onClearSearch: PropTypes.func.isRequired
};

export default AddNewItem;
