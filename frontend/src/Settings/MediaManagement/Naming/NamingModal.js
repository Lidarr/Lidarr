import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import SelectInput from 'Components/Form/SelectInput';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import NamingOption from './NamingOption';
import styles from './NamingModal.css';

const separatorOptions = [
  {
    key: ' ',
    get value() {
      return `${translate('Space')} ( )`;
    }
  },
  {
    key: '.',
    get value() {
      return `${translate('Period')} (.)`;
    }
  },
  {
    key: '_',
    get value() {
      return `${translate('Underscore')} (_)`;
    }
  },
  {
    key: '-',
    get value() {
      return `${translate('Dash')} (-)`;
    }
  }
];

const caseOptions = [
  {
    key: 'title',
    get value() {
      return translate('DefaultCase');
    }
  },
  {
    key: 'lower',
    get value() {
      return translate('Lowercase');
    }
  },
  {
    key: 'upper',
    get value() {
      return translate('Uppercase');
    }
  }
];

const fileNameTokens = [
  {
    token: '{Artist Name} - {Album Title} - {track:00} - {Track Title} {Quality Full}',
    example: 'Artist Name - Album Title - 01 - Track Title MP3-320 Proper'
  },
  {
    token: '{Artist.Name}.{Album.Title}.{track:00}.{TrackClean.Title}.{Quality.Full}',
    example: 'Artist.Name.Album.Title.01.Track.Title.MP3-320'
  }
];

const artistTokens = [
  { token: '{Artist Name}', example: 'Artist Name' },
  { token: '{Artist CleanName}', example: 'Artist Name' },
  { token: '{Artist NameThe}', example: 'Artist Name, The' },
  { token: '{Artist CleanNameThe}', example: 'Artist Name, The' },
  { token: '{Artist NameFirstCharacter}', example: 'A' },
  { token: '{Artist Disambiguation}', example: 'Disambiguation' },
  { token: '{Artist Genre}', example: 'Pop' },
  { token: '{Artist MbId}', example: 'db92a151-1ac2-438b-bc43-b82e149ddd50' }
];

const albumTokens = [
  { token: '{Album Title}', example: 'Album Title' },
  { token: '{Album CleanTitle}', example: 'Album Title' },
  { token: '{Album TitleThe}', example: 'Album Title, The' },
  { token: '{Album CleanTitleThe}', example: 'Album Title, The' },
  { token: '{Album Type}', example: 'Album Type' },
  { token: '{Album Disambiguation}', example: 'Disambiguation' },
  { token: '{Album Genre}', example: 'Rock' },
  { token: '{Album MbId}', example: '082c6aff-a7cc-36e0-a960-35a578ecd937' }
];

const mediumTokens = [
  { token: '{medium:0}', example: '1' },
  { token: '{medium:00}', example: '01' }
];

const mediumFormatTokens = [
  { token: '{Medium Name}', example: 'First Medium' },
  { token: '{Medium Format}', example: 'CD' }
];

const trackTokens = [
  { token: '{track:0}', example: '1' },
  { token: '{track:00}', example: '01' }
];

const releaseDateTokens = [
  { token: '{Release Year}', example: '2016' },
  { token: '{Release Date}', example: '2016-01-31' }
];

const trackTitleTokens = [
  { token: '{Track Title}', example: 'Track Title' },
  { token: '{Track CleanTitle}', example: 'Track Title' }
];

const trackArtistTokens = [
  { token: '{Track ArtistName}', example: 'Artist Name' },
  { token: '{Track ArtistCleanName}', example: 'Artist Name' },
  { token: '{Track ArtistNameThe}', example: 'Artist Name, The' },
  { token: '{Track ArtistCleanNameThe}', example: 'Artist Name, The' },
  { token: '{Track ArtistMbId}', example: 'db92a151-1ac2-438b-bc43-b82e149ddd50' }
];

const qualityTokens = [
  { token: '{Quality Full}', example: 'FLAC Proper' },
  { token: '{Quality Title}', example: 'FLAC' }
];

const mediaInfoTokens = [
  { token: '{MediaInfo AudioCodec}', example: 'FLAC' },
  { token: '{MediaInfo AudioChannels}', example: '2.0' },
  { token: '{MediaInfo AudioBitRate}', example: '320kbps' },
  { token: '{MediaInfo AudioBitsPerSample}', example: '24bit' },
  { token: '{MediaInfo AudioSampleRate}', example: '44.1kHz' }
];

const otherTokens = [
  { token: '{Release Group}', example: 'Rls Grp' },
  { token: '{Custom Formats}', example: 'iNTERNAL' }
];

const originalTokens = [
  { token: '{Original Title}', example: 'Artist.Name.Album.Name.2018.FLAC-EVOLVE' },
  { token: '{Original Filename}', example: '01 - track name' }
];

class NamingModal extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this._selectionStart = null;
    this._selectionEnd = null;

    this.state = {
      separator: ' ',
      case: 'title'
    };
  }

  //
  // Listeners

  onTokenSeparatorChange = (event) => {
    this.setState({ separator: event.value });
  };

  onTokenCaseChange = (event) => {
    this.setState({ case: event.value });
  };

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

    if (isFullFilename) {
      onInputChange({ name, value: tokenValue });
    } else if (selectionStart == null) {
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

  render() {
    const {
      name,
      value,
      isOpen,
      advancedSettings,
      album,
      track,
      additional,
      onInputChange,
      onModalClose
    } = this.props;

    const {
      separator: tokenSeparator,
      case: tokenCase
    } = this.state;

    return (
      <Modal
        isOpen={isOpen}
        onModalClose={onModalClose}
      >
        <ModalContent onModalClose={onModalClose}>
          <ModalHeader>
            {translate('FileNameTokens')}
          </ModalHeader>

          <ModalBody>
            <div className={styles.namingSelectContainer}>
              <SelectInput
                className={styles.namingSelect}
                name="separator"
                value={tokenSeparator}
                values={separatorOptions}
                onChange={this.onTokenSeparatorChange}
              />

              <SelectInput
                className={styles.namingSelect}
                name="case"
                value={tokenCase}
                values={caseOptions}
                onChange={this.onTokenCaseChange}
              />
            </div>

            {
              !advancedSettings &&
                <FieldSet legend={translate('FileNames')}>
                  <div className={styles.groups}>
                    {
                      fileNameTokens.map(({ token, example }) => {
                        return (
                          <NamingOption
                            key={token}
                            name={name}
                            value={value}
                            token={token}
                            example={example}
                            isFullFilename={true}
                            tokenSeparator={tokenSeparator}
                            tokenCase={tokenCase}
                            size={sizes.LARGE}
                            onPress={this.onOptionPress}
                          />
                        );
                      }
                      )
                    }
                  </div>
                </FieldSet>
            }

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
                        tokenSeparator={tokenSeparator}
                        tokenCase={tokenCase}
                        onPress={this.onOptionPress}
                      />
                    );
                  }
                  )
                }
              </div>
            </FieldSet>

            {
              album &&
                <div>
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
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('ReleaseDate')}>
                    <div className={styles.groups}>
                      {
                        releaseDateTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>
                </div>
            }

            {
              track &&
                <div>
                  <FieldSet legend={translate('Medium')}>
                    <div className={styles.groups}>
                      {
                        mediumTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('MediumFormat')}>
                    <div className={styles.groups}>
                      {
                        mediumFormatTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('Track')}>
                    <div className={styles.groups}>
                      {
                        trackTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                </div>
            }

            {
              additional &&
                <div>
                  <FieldSet legend={translate('TrackTitle')}>
                    <div className={styles.groups}>
                      {
                        trackTitleTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('TrackArtist')}>
                    <div className={styles.groups}>
                      {
                        trackArtistTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('Quality')}>
                    <div className={styles.groups}>
                      {
                        qualityTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('MediaInfo')}>
                    <div className={styles.groups}>
                      {
                        mediaInfoTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('Other')}>
                    <div className={styles.groups}>
                      {
                        otherTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('Original')}>
                    <div className={styles.groups}>
                      {
                        originalTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              size={sizes.LARGE}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>
                </div>
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

NamingModal.propTypes = {
  name: PropTypes.string.isRequired,
  value: PropTypes.string.isRequired,
  isOpen: PropTypes.bool.isRequired,
  advancedSettings: PropTypes.bool.isRequired,
  album: PropTypes.bool.isRequired,
  track: PropTypes.bool.isRequired,
  additional: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

NamingModal.defaultProps = {
  album: false,
  track: false,
  additional: false
};

export default NamingModal;
