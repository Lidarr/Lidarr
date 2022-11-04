import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Scroller from 'Components/Scroller/Scroller';
import { scrollDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import SelectArtistRow from './SelectArtistRow';
import styles from './SelectArtistModalContent.css';

class SelectArtistModalContent extends Component {

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
    this.setState({ filter: value.toLowerCase() });
  };

  //
  // Render

  render() {
    const {
      items,
      onArtistSelect,
      onModalClose
    } = this.props;

    const filter = this.state.filter;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('ManualImport')} - {translate('SelectArtist')}
        </ModalHeader>

        <ModalBody
          className={styles.modalBody}
          scrollDirection={scrollDirections.NONE}
        >
          <TextInput
            className={styles.filterInput}
            placeholder={translate('FilterPlaceHolder')}
            name="filter"
            value={filter}
            autoFocus={true}
            onChange={this.onFilterChange}
          />

          <Scroller className={styles.scroller}>
            {
              items.map((item) => {
                return item.artistName.toLowerCase().includes(filter) ?
                  (
                    <SelectArtistRow
                      key={item.id}
                      id={item.id}
                      artistName={item.artistName}
                      onArtistSelect={onArtistSelect}
                    />
                  ) :
                  null;
              })
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

SelectArtistModalContent.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  onArtistSelect: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default SelectArtistModalContent;
