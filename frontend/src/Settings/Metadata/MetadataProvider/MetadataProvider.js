import PropTypes from 'prop-types';
import React from 'react';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes } from 'Helpers/Props';
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
          <div>
            {translate('UnableToLoadMetadataProviderSettings')}
          </div>
      }

      {
        hasSettings && !isFetching && !error &&
          <Form>
            {
              advancedSettings &&
                <FieldSet legend={translate('MetadataProviderSource')}>
                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                  >
                    <FormLabel>
                      {translate('MetadataSource')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.TEXT}
                      name="metadataSource"
                      helpText={translate('MetadataSourceHelpText')}
                      helpLink="https://wiki.servarr.com/lidarr/settings#metadata"
                      onChange={onInputChange}
                      {...settings.metadataSource}
                    />
                  </FormGroup>
                </FieldSet>
            }

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
