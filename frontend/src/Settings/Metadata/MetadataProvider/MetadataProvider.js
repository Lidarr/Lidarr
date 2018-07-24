import PropTypes from 'prop-types';
import React from 'react';
import { inputTypes } from 'Helpers/Props';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormLabel from 'Components/Form/FormLabel';
import FormInputGroup from 'Components/Form/FormInputGroup';

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
          <div>Unable to load Metadata Provider settings</div>
      }

      {
        hasSettings && !isFetching && !error &&
          <Form>
            {
              advancedSettings &&
              <FieldSet legend="Metadata Provider Source">
                <FormGroup
                  advancedSettings={advancedSettings}
                  isAdvanced={true}
                >
                  <FormLabel>Metadata Source</FormLabel>

                  <FormInputGroup
                    type={inputTypes.TEXT}
                    name="metadataSource"
                    helpText="Alternative Metadata Source (Leave blank for default)"
                    helpLink="https://github.com/Lidarr/Lidarr/wiki/Metadata-Source"
                    onChange={onInputChange}
                    {...settings.metadataSource}
                  />
                </FormGroup>
              </FieldSet>
            }
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
