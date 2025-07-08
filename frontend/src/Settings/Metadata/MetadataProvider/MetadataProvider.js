import PropTypes from 'prop-types';
import React from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

const writeAudioTagOptions = [
  { key: 'sync', value: 'All files; keep in sync with MusicBrainz' },
  { key: 'allFiles', value: 'All files; initial import only' },
  { key: 'newFiles', value: 'For new downloads only' },
  { key: 'no', value: 'Never' }
];

function MetadataProvider(props) {
  const {
    advancedSettings,
    isFetching,
    error,
    settings,
    hasSettings,
    onInputChange
  } = props;

  return (

    <div>
      {
        isFetching &&
          <LoadingIndicator />
      }

      {
        !isFetching && error &&
          <Alert kind={kinds.DANGER}>
            {translate('UnableToLoadMetadataProviderSettings')}
          </Alert>
      }

      {
        hasSettings && !isFetching && !error &&
          <Form>
            <FieldSet legend={translate('WriteMetadataToAudioFiles')}>
              <FormGroup>
                <FormLabel>
                  {translate('TagAudioFilesWithMetadata')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="writeAudioTags"
                  helpTextWarning={translate('WriteAudioTagsHelpTextWarning')}
                  helpLink="https://wiki.servarr.com/lidarr/settings#write-metadata-to-audio-files"
                  values={writeAudioTagOptions}
                  onChange={onInputChange}
                  {...settings.writeAudioTags}
                />
              </FormGroup>

              {
                settings.writeAudioTags.value !== 'no' &&
                  <FormGroup>
                    <FormLabel>
                      {translate('EmbedCoverArtInAudioFiles')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="embedCoverArt"
                      helpText={translate('EmbedCoverArtHelpText')}
                      onChange={onInputChange}
                      {...settings.embedCoverArt}
                    />
                  </FormGroup>
              }

              <FormGroup>
                <FormLabel>
                  {translate('ScrubExistingTags')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="scrubAudioTags"
                  helpText={translate('ScrubAudioTagsHelpText')}
                  onChange={onInputChange}
                  {...settings.scrubAudioTags}
                />
              </FormGroup>

              <FormGroup
                advancedSettings={advancedSettings}
                isAdvanced={true}
              >
                <FormLabel>
                  {translate('MetadataSourceUrl')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="metadataSource"
                  helpText={translate('MetadataSourceUrlHelpText')}
                  onChange={onInputChange}
                  placeholder="https://api.lidarr.audio/api/v0.4/"
                  {...settings.metadataSource}
                />
              </FormGroup>

            </FieldSet>
          </Form>
      }
    </div>

  );
}

MetadataProvider.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default MetadataProvider;
