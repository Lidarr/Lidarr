import PropTypes from 'prop-types';
import React, { Component } from 'react';
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
import { inputTypes, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

class EditAlbumModalContent extends Component {

  //
  // Listeners

  onSavePress = () => {
    const {
      onSavePress
    } = this.props;

    onSavePress(false);

  };

  //
  // Render

  render() {
    const {
      title,
      artistName,
      albumType,
      statistics = {},
      item,
      isSaving,
      onInputChange,
      onModalClose,
      ...otherProps
    } = this.props;

    const {
      trackFileCount = 0
    } = statistics;

    const {
      monitored,
      anyReleaseOk,
      releases
    } = item;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Edit - {artistName} - {title} [{albumType}]
        </ModalHeader>

        <ModalBody>
          <Form
            {...otherProps}
          >
            <FormGroup size={sizes.MEDIUM}>
              <FormLabel>
                {translate('Monitored')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="monitored"
                helpText={translate('MonitoredHelpText')}
                {...monitored}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup size={sizes.MEDIUM}>
              <FormLabel>
                {translate('AutomaticallySwitchRelease')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="anyReleaseOk"
                helpText={translate('AnyReleaseOkHelpText')}
                {...anyReleaseOk}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup size={sizes.MEDIUM}>
              <FormLabel>
                {translate('Release')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.ALBUM_RELEASE_SELECT}
                name="releases"
                helpText={translate('ReleasesHelpText')}
                isDisabled={anyReleaseOk.value && trackFileCount > 0}
                albumReleases={releases}
                onChange={onInputChange}
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

EditAlbumModalContent.propTypes = {
  albumId: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  artistName: PropTypes.string.isRequired,
  albumType: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  item: PropTypes.object.isRequired,
  isSaving: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default EditAlbumModalContent;
