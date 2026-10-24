import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputButton from 'Components/Form/FormInputButton';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import SearchFormatModal from './SearchFormatModal';
import styles from './SearchFormat.css';

class SearchFormat extends Component {

  //
  // Lifecycle
  //

  constructor(props, context) {
    super(props, context);

    this.state = {
      isModalOpen: false,
      modalOptions: null
    };
  }

  //
  // Listeners
  //

  onAlbumFormatModalOpenClick = () => {
    this.setState({
      isModalOpen: true,
      modalOptions: {
        name: 'albumSearchFormat',
        isAlbum: true
      }
    });
  };

  onArtistFormatModalOpenClick = () => {
    this.setState({
      isModalOpen: true,
      modalOptions: {
        name: 'artistSearchFormat',
        isAlbum: false
      }
    });
  };

  onModalClose = () => {
    this.setState({ isModalOpen: false });
  };

  //
  // Render
  //

  render() {
    const {
      isFetching,
      error,
      settings,
      hasSettings,
      examples,
      examplesPopulated,
      onInputChange
    } = this.props;

    const {
      isModalOpen,
      modalOptions
    } = this.state;

    const useCustomSearchFormat = hasSettings && settings.useCustomSearchFormat.value;

    const albumSearchFormatHelpTexts = [];
    const artistSearchFormatHelpTexts = [];

    if (examplesPopulated) {
      if (examples.albumSearchExample) {
        albumSearchFormatHelpTexts.push(`Example: ${examples.albumSearchExample}`);
      }
      if (examples.artistSearchExample) {
        artistSearchFormatHelpTexts.push(`Example: ${examples.artistSearchExample}`);
      }
    }

    return (
      <FieldSet legend={translate('SearchSchema')}>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && error &&
            <Alert kind={kinds.DANGER}>
              {translate('UnableToLoadSearchFormatSettings')}
            </Alert>
        }

        {
          hasSettings && !isFetching && !error &&
            <Form>
              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>
                  {translate('UseCustomSearchFormat')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="useCustomSearchFormat"
                  helpText={translate('UseCustomSearchFormatHelpText')}
                  onChange={onInputChange}
                  {...settings.useCustomSearchFormat}
                />
              </FormGroup>

              {
                useCustomSearchFormat &&
                  <div>
                    <FormGroup size={sizes.LARGE}>
                      <FormLabel>
                        {translate('AlbumSearchFormat')}
                      </FormLabel>

                      <FormInputGroup
                        inputClassName={styles.searchFormatInput}
                        type={inputTypes.TEXT}
                        name="albumSearchFormat"
                        buttons={<FormInputButton onPress={this.onAlbumFormatModalOpenClick}>?</FormInputButton>}
                        onChange={onInputChange}
                        {...settings.albumSearchFormat}
                        helpTexts={albumSearchFormatHelpTexts}
                      />
                    </FormGroup>

                    <FormGroup size={sizes.LARGE}>
                      <FormLabel>
                        {translate('ArtistSearchFormat')}
                      </FormLabel>

                      <FormInputGroup
                        inputClassName={styles.searchFormatInput}
                        type={inputTypes.TEXT}
                        name="artistSearchFormat"
                        buttons={<FormInputButton onPress={this.onArtistFormatModalOpenClick}>?</FormInputButton>}
                        onChange={onInputChange}
                        {...settings.artistSearchFormat}
                        helpTexts={artistSearchFormatHelpTexts}
                      />
                    </FormGroup>
                  </div>
              }

              {
                modalOptions &&
                  <SearchFormatModal
                    isOpen={isModalOpen}
                    {...modalOptions}
                    value={(settings[modalOptions.name] && settings[modalOptions.name].value) || ''}
                    onInputChange={onInputChange}
                    onModalClose={this.onModalClose}
                  />
              }
            </Form>
        }
      </FieldSet>
    );
  }
}

SearchFormat.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  examples: PropTypes.object.isRequired,
  examplesPopulated: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default SearchFormat;
