import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Scroller from 'Components/Scroller/Scroller';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { scrollDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import SelectAlbumRow from './SelectAlbumRow';
import styles from './SelectAlbumModalContent.css';

const columns = [
  {
    name: 'title',
    label: () => translate('AlbumTitle'),
    isVisible: true
  },
  {
    name: 'albumType',
    label: () => translate('AlbumType'),
    isVisible: true
  },
  {
    name: 'releaseDate',
    label: () => translate('ReleaseDate'),
    isVisible: true
  },
  {
    name: 'status',
    label: () => translate('AlbumStatus'),
    isVisible: true
  }
];

class SelectAlbumModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      filter: ''
    };
  }

  //
  // Listeners

  onFilterChange = ({ value }) => {
    this.setState({ filter: value });
  };

  //
  // Render

  render() {
    const {
      items,
      onAlbumSelect,
      onModalClose,
      isFetching,
      ...otherProps
    } = this.props;

    const filter = this.state.filter;
    const filterLower = filter.toLowerCase();

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Manual Import - Select Album
        </ModalHeader>

        <ModalBody
          className={styles.modalBody}
          scrollDirection={scrollDirections.NONE}
        >
          {
            isFetching &&
              <LoadingIndicator />
          }
          <TextInput
            className={styles.filterInput}
            placeholder={translate('FilterAlbumPlaceholder')}
            name="filter"
            value={filter}
            autoFocus={true}
            onChange={this.onFilterChange}
          />

          <Scroller
            className={styles.scroller}
            autoFocus={false}
          >
            {
              <Table
                columns={columns}
                {...otherProps}
              >
                <TableBody>
                  {
                    items.map((item) => {
                      return item.title.toLowerCase().includes(filterLower) ?
                        (
                          <SelectAlbumRow
                            key={item.id}
                            columns={columns}
                            onAlbumSelect={onAlbumSelect}
                            {...item}
                          />
                        ) :
                        null;
                    })
                  }
                </TableBody>
              </Table>
            }
          </Scroller>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            {translate('Cancel')}
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

SelectAlbumModalContent.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  isFetching: PropTypes.bool.isRequired,
  onAlbumSelect: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default SelectAlbumModalContent;
