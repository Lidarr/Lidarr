import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import { kinds, icons } from 'Helpers/Props';
import Icon from 'Components/Icon';
import SpinnerButton from 'Components/Link/SpinnerButton';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import CheckInput from 'Components/Form/CheckInput';
import ModalContent from 'Components/Modal/ModalContent';
import ModalHeader from 'Components/Modal/ModalHeader';
import ModalBody from 'Components/Modal/ModalBody';
import ModalFooter from 'Components/Modal/ModalFooter';
import AlbumCover from 'Album/AlbumCover';
import AddArtistOptionsForm from '../Common/AddArtistOptionsForm.js';
import styles from './AddNewAlbumModalContent.css';

const columns = [
  {
    name: 'title',
    label: 'Title',
    isSortable: false,
    isVisible: true
  },
  {
    name: 'format',
    label: 'Format',
    isSortable: false,
    isVisible: true
  },
  {
    name: 'tracks',
    label: 'Tracks',
    isSortable: false,
    isVisible: true
  },
  {
    name: 'country',
    label: 'Country',
    isSortable: false,
    isVisible: true
  },
  {
    name: 'label',
    label: 'Label',
    isSortable: false,
    isVisible: true
  }
];

function ReleasesTable(props) {
  return (
    <Table
      columns={columns}
    >
      <TableBody>
        {
          props.releases.map((item, i) => {
            return (
              <TableRow key={i}>
                <TableRowCell key='title'>
                  {item.title}
                </TableRowCell>
                <TableRowCell key='format'>
                  {item.format}
                </TableRowCell>
                <TableRowCell key='tracks'>
                  {item.trackCount}
                </TableRowCell>
                <TableRowCell key='country'>
                  {_.join(item.country, ', ')}
                </TableRowCell>
                <TableRowCell key='label'>
                  {_.join(item.label, ', ')}
                </TableRowCell>
              </TableRow>
            );
          })
        }
      </TableBody>
    </Table>
  );
}

ReleasesTable.propTypes = {
  releases: PropTypes.arrayOf(PropTypes.object)
};

class AddNewAlbumModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      searchForNewAlbum: false,
      expandReleases: false
    };
  }

  //
  // Listeners

  onSearchForNewAlbumChange = ({ value }) => {
    this.setState({ searchForNewAlbum: value });
  }

  onExpandReleasesPress = () => {
    this.setState((prevState) => ({
      expandReleases: !prevState.expandReleases
    }));
  }

  onAddAlbumPress = () => {
    this.props.onAddAlbumPress(this.state.searchForNewAlbum);
  }

  //
  // Render

  render() {
    const {
      albumTitle,
      artistName,
      disambiguation,
      overview,
      images,
      releases,
      isAdding,
      isExistingArtist,
      isSmallScreen,
      onModalClose,
      ...otherProps
    } = this.props;

    const {
      expandReleases
    } = this.state;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Add new Album
        </ModalHeader>

        <ModalBody>
          <div className={styles.container}>
            {
              isSmallScreen ?
                null:
                <div className={styles.poster}>
                  <AlbumCover
                    className={styles.poster}
                    images={images}
                    size={250}
                  />
                </div>
            }

            <div className={styles.info}>
              <div className={styles.name}>
                {albumTitle}
              </div>

              {
                !!disambiguation &&
                  <span className={styles.disambiguation}>({disambiguation})</span>
              }

              <div>
                <span className={styles.artistName}> By: {artistName}</span>
              </div>

              {
                overview ?
                  <div className={styles.overview}>
                    <TextTruncate
                      truncateText="…"
                      line={8}
                      text={overview}
                    />
                  </div> :
                  null
              }

              <div
                className={styles.albumType}
              >
                <div className={styles.header} onClick={this.onExpandReleasesPress}>
                  <div className={styles.left}>
                    {
                      <div>
                        <span className={styles.albumTypeLabel}>
                          Releases
                        </span>

                        <span className={styles.albumCount}>
                          ({releases.length} versions)
                        </span>
                      </div>
                    }

                  </div>

                  <Icon
                    className={styles.expandButtonIcon}
                    name={expandReleases ? icons.COLLAPSE : icons.EXPAND}
                    title={expandReleases ? 'Hide releases' : 'Show releases'}
                    size={24}
                  />

                  {
                    !isSmallScreen &&
                      <span>&nbsp;</span>
                  }

                </div>

                {
                  expandReleases &&
                    <ReleasesTable
                      releases={releases}
                    />
                }
              </div>

              {
                !isExistingArtist &&
                  <AddArtistOptionsForm
                    artistName={artistName}
                    includeNoneMetadataProfile={true}
                    {...otherProps}
                  />
              }
            </div>
          </div>
        </ModalBody>

        <ModalFooter className={styles.modalFooter}>
          <label className={styles.searchForNewAlbumLabelContainer}>
            <span className={styles.searchForNewAlbumLabel}>
              Start search for new album
            </span>

            <CheckInput
              containerClassName={styles.searchForNewAlbumContainer}
              className={styles.searchForNewAlbumInput}
              name="searchForNewAlbum"
              value={this.state.searchForNewAlbum}
              onChange={this.onSearchForNewAlbumChange}
            />
          </label>

          <SpinnerButton
            className={styles.addButton}
            kind={kinds.SUCCESS}
            isSpinning={isAdding}
            onPress={this.onAddAlbumPress}
          >
            Add {albumTitle}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AddNewAlbumModalContent.propTypes = {
  albumTitle: PropTypes.string.isRequired,
  artistName: PropTypes.string.isRequired,
  disambiguation: PropTypes.string.isRequired,
  overview: PropTypes.string,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  releases: PropTypes.arrayOf(PropTypes.object).isRequired,
  isAdding: PropTypes.bool.isRequired,
  addError: PropTypes.object,
  isExistingArtist: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onAddAlbumPress: PropTypes.func.isRequired
};

export default AddNewAlbumModalContent;
