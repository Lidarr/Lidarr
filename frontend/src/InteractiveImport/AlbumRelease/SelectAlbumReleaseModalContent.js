import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Scroller from 'Components/Scroller/Scroller';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { scrollDirections } from 'Helpers/Props';
import SelectAlbumReleaseRow from './SelectAlbumReleaseRow';
import styles from './SelectAlbumReleaseModalContent.css';

const columns = [
  {
    name: 'album',
    label: 'Album',
    isVisible: true
  },
  {
    name: 'release',
    label: 'Album Release',
    isVisible: true
  }
];

class SelectAlbumReleaseModalContent extends Component {

  //
  // Render

  render() {
    const {
      albums,
      onAlbumReleaseSelect,
      onModalClose,
      ...otherProps
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Manual Import - Select Album Release
        </ModalHeader>

        <ModalBody
          className={styles.modalBody}
          scrollDirection={scrollDirections.NONE}
        >
          <Alert>
            Overriding a release here will <b>disable automatic release selection</b> for that album in future.
          </Alert>

          <Scroller className={styles.scroller}>
            {
              <Table
                columns={columns}
                {...otherProps}
              >
                <TableBody>
                  {
                    albums.map((item) => {
                      return (
                        <SelectAlbumReleaseRow
                          key={item.album.id}
                          matchedReleaseId={item.matchedReleaseId}
                          columns={columns}
                          onAlbumReleaseSelect={onAlbumReleaseSelect}
                          {...item.album}
                        />
                      );
                    })
                  }
                </TableBody>
              </Table>
            }
          </Scroller>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            Cancel
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

SelectAlbumReleaseModalContent.propTypes = {
  albums: PropTypes.arrayOf(PropTypes.object).isRequired,
  onAlbumReleaseSelect: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default SelectAlbumReleaseModalContent;
