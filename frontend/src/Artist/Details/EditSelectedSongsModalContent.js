import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

const NO_CHANGE = 'noChange';
const MONITORED = 'monitored';
const UNMONITORED = 'unmonitored';

function translateWithFallback(key, fallback) {
  const translated = translate(key);
  return translated === key ? fallback : translated;
}

class EditSelectedSongsModalContent extends Component {

  constructor(props, context) {
    super(props, context);

    this.state = {
      monitoring: NO_CHANGE
    };
  }

  onInputChange = ({ value }) => {
    this.setState({ monitoring: value });
  };

  onSavePress = () => {
    this.props.onSavePress(this.state.monitoring === MONITORED);
  };

  render() {
    const {
      artistName,
      selectedCount,
      onModalClose
    } = this.props;

    const {
      monitoring
    } = this.state;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('Edit')} - {artistName} - {selectedCount} {translate('Songs')}
        </ModalHeader>

        <ModalBody>
          <Form>
            <FormGroup size={sizes.MEDIUM}>
              <FormLabel>
                {translate('Monitoring')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.SELECT}
                name="monitoring"
                value={monitoring}
                values={[
                  { key: NO_CHANGE, value: translate('NoChange') },
                  { key: MONITORED, value: translate('Monitored') },
                  { key: UNMONITORED, value: translate('Unmonitored') }
                ]}
                helpText={translateWithFallback(
                  'SelectedSongsMonitoringHelpText',
                  'Choose whether the selected songs should be monitored'
                )}
                onChange={this.onInputChange}
              />
            </FormGroup>
          </Form>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            {translate('Cancel')}
          </Button>

          <Button
            isDisabled={monitoring === NO_CHANGE}
            onPress={this.onSavePress}
          >
            {translate('Save')}
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

EditSelectedSongsModalContent.propTypes = {
  artistName: PropTypes.string.isRequired,
  selectedCount: PropTypes.number.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default EditSelectedSongsModalContent;
