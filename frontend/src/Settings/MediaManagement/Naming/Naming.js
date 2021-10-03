import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputButton from 'Components/Form/FormInputButton';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import NamingModal from './NamingModal';
import styles from './Naming.css';

class Naming extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isNamingModalOpen: false,
      namingModalOptions: null
    };
  }

  //
  // Listeners

  onStandardNamingModalOpenClick = () => {
    this.setState({
      isNamingModalOpen: true,
      namingModalOptions: {
        name: 'standardTrackFormat',
        album: true,
        track: true,
        additional: true
      }
    });
  }

  onMultiDiscNamingModalOpenClick = () => {
    this.setState({
      isNamingModalOpen: true,
      namingModalOptions: {
        name: 'multiDiscTrackFormat',
        album: true,
        track: true,
        additional: true
      }
    });
  }

  onArtistFolderNamingModalOpenClick = () => {
    this.setState({
      isNamingModalOpen: true,
      namingModalOptions: {
        name: 'artistFolderFormat'
      }
    });
  }

  onNamingModalClose = () => {
    this.setState({ isNamingModalOpen: false });
  }

  //
  // Render

  render() {
    const {
      advancedSettings,
      isFetching,
      error,
      settings,
      hasSettings,
      examples,
      examplesPopulated,
      onInputChange
    } = this.props;

    const {
      isNamingModalOpen,
      namingModalOptions
    } = this.state;

    const renameTracks = hasSettings && settings.renameTracks.value;

    const standardTrackFormatHelpTexts = [];
    const standardTrackFormatErrors = [];
    const multiDiscTrackFormatHelpTexts = [];
    const multiDiscTrackFormatErrors = [];
    const artistFolderFormatHelpTexts = [];
    const artistFolderFormatErrors = [];

    if (examplesPopulated) {
      if (examples.singleTrackExample) {
        standardTrackFormatHelpTexts.push(`Single Track: ${examples.singleTrackExample}`);
      } else {
        standardTrackFormatErrors.push({ message: 'Single Track: Invalid Format' });
      }

      if (examples.multiDiscTrackExample) {
        multiDiscTrackFormatHelpTexts.push(`Multi Disc Track: ${examples.multiDiscTrackExample}`);
      } else {
        multiDiscTrackFormatErrors.push({ message: 'Single Track: Invalid Format' });
      }

      if (examples.artistFolderExample) {
        artistFolderFormatHelpTexts.push(`Example: ${examples.artistFolderExample}`);
      } else {
        artistFolderFormatErrors.push({ message: 'Invalid Format' });
      }
    }

    return (
      <FieldSet legend={translate('TrackNaming')}>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && error &&
            <div>
              {translate('UnableToLoadNamingSettings')}
            </div>
        }

        {
          hasSettings && !isFetching && !error &&
            <Form>
              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>
                  {translate('RenameTracks')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="renameTracks"
                  helpText={translate('RenameTracksHelpText')}
                  onChange={onInputChange}
                  {...settings.renameTracks}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>
                  {translate('ReplaceIllegalCharacters')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="replaceIllegalCharacters"
                  helpText={translate('ReplaceIllegalCharactersHelpText')}
                  onChange={onInputChange}
                  {...settings.replaceIllegalCharacters}
                />
              </FormGroup>

              {
                renameTracks &&
                  <div>
                    <FormGroup size={sizes.LARGE}>
                      <FormLabel>
                        {translate('StandardTrackFormat')}
                      </FormLabel>

                      <FormInputGroup
                        inputClassName={styles.namingInput}
                        type={inputTypes.TEXT}
                        name="standardTrackFormat"
                        buttons={<FormInputButton onPress={this.onStandardNamingModalOpenClick}>?</FormInputButton>}
                        onChange={onInputChange}
                        {...settings.standardTrackFormat}
                        helpTexts={standardTrackFormatHelpTexts}
                        errors={[...standardTrackFormatErrors, ...settings.standardTrackFormat.errors]}
                      />
                    </FormGroup>

                    <FormGroup size={sizes.LARGE}>
                      <FormLabel>
                        {translate('MultiDiscTrackFormat')}
                      </FormLabel>

                      <FormInputGroup
                        inputClassName={styles.namingInput}
                        type={inputTypes.TEXT}
                        name="multiDiscTrackFormat"
                        buttons={<FormInputButton onPress={this.onMultiDiscNamingModalOpenClick}>?</FormInputButton>}
                        onChange={onInputChange}
                        {...settings.multiDiscTrackFormat}
                        helpTexts={multiDiscTrackFormatHelpTexts}
                        errors={[...multiDiscTrackFormatErrors, ...settings.multiDiscTrackFormat.errors]}
                      />
                    </FormGroup>

                  </div>
              }

              <FormGroup
                advancedSettings={advancedSettings}
                isAdvanced={true}
              >
                <FormLabel>
                  {translate('ArtistFolderFormat')}
                </FormLabel>

                <FormInputGroup
                  inputClassName={styles.namingInput}
                  type={inputTypes.TEXT}
                  name="artistFolderFormat"
                  buttons={<FormInputButton onPress={this.onArtistFolderNamingModalOpenClick}>?</FormInputButton>}
                  onChange={onInputChange}
                  {...settings.artistFolderFormat}
                  helpTexts={['Used when adding a new artist or moving an artist via the artist editor', ...artistFolderFormatHelpTexts]}
                  errors={[...artistFolderFormatErrors, ...settings.artistFolderFormat.errors]}
                />
              </FormGroup>

              {
                namingModalOptions &&
                  <NamingModal
                    isOpen={isNamingModalOpen}
                    advancedSettings={advancedSettings}
                    {...namingModalOptions}
                    value={settings[namingModalOptions.name].value}
                    onInputChange={onInputChange}
                    onModalClose={this.onNamingModalClose}
                  />
              }
            </Form>
        }
      </FieldSet>
    );
  }

}

Naming.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  examples: PropTypes.object.isRequired,
  examplesPopulated: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default Naming;
