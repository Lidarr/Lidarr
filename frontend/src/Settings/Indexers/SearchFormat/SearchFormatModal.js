import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import NamingOption from 'Settings/MediaManagement/Naming/NamingOption';
import translate from 'Utilities/String/translate';
import styles from './SearchFormatModal.css';

const artistTokens = [
  { token: '{Artist Name}', example: 'The Artist Name' },
  { token: '{Artist CleanName}', example: 'Artist+Name' }
];

const albumTokens = [
  { token: '{Album Title}', example: 'The Album Title' },
  { token: '{Album CleanTitle}', example: 'Album+Title' },
  { token: '{Album Year}', example: '2026' },
  { token: '{Album Disambiguation}', example: 'Deluxe Edition' }
];

class SearchFormatModal extends Component {

  //
  // Lifecycle
  //

  constructor(props, context) {
    super(props, context);

    this._selectionStart = null;
    this._selectionEnd = null;
  }

  //
  // Listeners
  //

  onInputSelectionChange = (selectionStart, selectionEnd) => {
    this._selectionStart = selectionStart;
    this._selectionEnd = selectionEnd;
  };

  onOptionPress = ({ isFullFilename, tokenValue }) => {
    const {
      name,
      value,
      onInputChange
    } = this.props;

    const selectionStart = this._selectionStart;
    const selectionEnd = this._selectionEnd;

    if (selectionStart == null) {
      onInputChange({
        name,
        value: `${value}${tokenValue}`
      });
    } else {
      const start = value.substring(0, selectionStart);
      const end = value.substring(selectionEnd);
      const newValue = `${start}${tokenValue}${end}`;

      onInputChange({ name, value: newValue });
      this._selectionStart = newValue.length - 1;
      this._selectionEnd = newValue.length - 1;
    }
  };

  //
  // Render
  //

  render() {
    const {
      name,
      value,
      isOpen,
      isAlbum,
      onModalClose,
      onInputChange
    } = this.props;

    return (
      <Modal
        isOpen={isOpen}
        onModalClose={onModalClose}
      >
        <ModalContent onModalClose={onModalClose}>
          <ModalHeader>
            {translate('SearchFormatTokens')}
          </ModalHeader>

          <ModalBody>
            <FieldSet legend={translate('Artist')}>
              <div className={styles.groups}>
                {
                  artistTokens.map(({ token, example }) => {
                    return (
                      <NamingOption
                        key={token}
                        name={name}
                        value={value}
                        token={token}
                        example={example}
                        tokenSeparator=" "
                        tokenCase="title"
                        onPress={this.onOptionPress}
                      />
                    );
                  })
                }
              </div>
            </FieldSet>

            {
              isAlbum &&
                <FieldSet legend={translate('Album')}>
                  <div className={styles.groups}>
                    {
                      albumTokens.map(({ token, example }) => {
                        return (
                          <NamingOption
                            key={token}
                            name={name}
                            value={value}
                            token={token}
                            example={example}
                            tokenSeparator=" "
                            tokenCase="title"
                            onPress={this.onOptionPress}
                          />
                        );
                      })
                    }
                  </div>
                </FieldSet>
            }
          </ModalBody>

          <ModalFooter>
            <TextInput
              name={name}
              value={value}
              onChange={onInputChange}
              onSelectionChange={this.onInputSelectionChange}
            />
            <Button onPress={onModalClose}>
              {translate('Close')}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    );
  }
}

SearchFormatModal.propTypes = {
  name: PropTypes.string.isRequired,
  value: PropTypes.string.isRequired,
  isOpen: PropTypes.bool.isRequired,
  isAlbum: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default SearchFormatModal;
