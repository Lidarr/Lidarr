import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import translate from 'Utilities/String/translate';
import ArtistHistoryRowConnector from './ArtistHistoryRowConnector';

const columns = [
  {
    name: 'eventType',
    isVisible: true
  },
  {
    name: 'album',
    label: 'Album',
    isVisible: true
  },
  {
    name: 'sourceTitle',
    label: 'Source Title',
    isVisible: true
  },
  {
    name: 'quality',
    label: 'Quality',
    isVisible: true
  },
  {
    name: 'date',
    label: 'Date',
    isVisible: true
  },
  {
    name: 'details',
    label: 'Details',
    isVisible: true
  },
  {
    name: 'actions',
    label: 'Actions',
    isVisible: true
  }
];

class ArtistHistoryModalContent extends Component {

  //
  // Render

  render() {
    const {
      albumId,
      isFetching,
      isPopulated,
      error,
      items,
      onMarkAsFailedPress,
      onModalClose
    } = this.props;

    const fullArtist = albumId == null;
    const hasItems = !!items.length;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          History
        </ModalHeader>

        <ModalBody>
          {
            isFetching &&
              <LoadingIndicator />
          }

          {
            !isFetching && !!error &&
              <div>
                {translate('UnableToLoadHistory')}
              </div>
          }

          {
            isPopulated && !hasItems && !error &&
              <div>
                {translate('NoHistory')}
              </div>
          }

          {
            isPopulated && hasItems && !error &&
              <Table columns={columns}>
                <TableBody>
                  {
                    items.map((item) => {
                      return (
                        <ArtistHistoryRowConnector
                          key={item.id}
                          fullArtist={fullArtist}
                          {...item}
                          onMarkAsFailedPress={onMarkAsFailedPress}
                        />
                      );
                    })
                  }
                </TableBody>
              </Table>
          }
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            Close
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

ArtistHistoryModalContent.propTypes = {
  albumId: PropTypes.number,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  onMarkAsFailedPress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default ArtistHistoryModalContent;
