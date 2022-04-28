import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

const NO_CHANGE = 'noChange';

class MonitoringOptionsModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      monitor: NO_CHANGE
    };
  }

  componentDidUpdate(prevProps) {
    const {
      isSaving,
      saveError
    } = prevProps;

    if (prevProps.isSaving && !isSaving && !saveError) {
      this.setState({
        monitor: NO_CHANGE
      });
    }
  }

  onInputChange = ({ name, value }) => {
    this.setState({ [name]: value });
  };

  //
  // Listeners

  onSavePress = () => {
    const {
      onSavePress,
      isSaving
    } = this.props;
    const {
      monitor
    } = this.state;

    if (monitor !== NO_CHANGE) {
      onSavePress({ monitor });
    }

    if (!isSaving) {
      this.onModalClose();
    }
  };

  onModalClose = () => {
    this.props.onModalClose();
  };

  //
  // Render

  render() {
    const {
      isSaving,
      onInputChange,
      onModalClose,
      ...otherProps
    } = this.props;

    const {
      monitor
    } = this.state;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('MonitorAlbum')}
        </ModalHeader>

        <ModalBody>
          <Alert kind={kinds.INFO}>
            <div>
              {translate('MonitorAlbumExistingOnlyWarning')}
            </div>
          </Alert>

          <Form {...otherProps}>
            <FormGroup>
              <FormLabel>{translate('Monitoring')}</FormLabel>

              <FormInputGroup
                type={inputTypes.MONITOR_ALBUMS_SELECT}
                name="monitor"
                value={monitor}
                includeNoChange={true}
                onChange={this.onInputChange}
              />
            </FormGroup>
          </Form>
        </ModalBody>

        <ModalFooter>
          <Button
            onPress={onModalClose}
          >
            {translate('Cancel')}
          </Button>

          <SpinnerButton
            isSpinning={isSaving}
            onPress={this.onSavePress}
          >
            {translate('Save')}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

MonitoringOptionsModalContent.propTypes = {
  authorId: PropTypes.number.isRequired,
  saveError: PropTypes.object,
  isSaving: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

MonitoringOptionsModalContent.defaultProps = {
  isSaving: false
};

export default MonitoringOptionsModalContent;
