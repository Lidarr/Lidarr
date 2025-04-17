import PropTypes from 'prop-types';
import React, { Component } from 'react';
import ArtistMetadataProfilePopoverContent from 'AddArtist/ArtistMetadataProfilePopoverContent';
import ArtistMonitorNewItemsOptionsPopoverContent from 'AddArtist/ArtistMonitorNewItemsOptionsPopoverContent';
import MoveArtistModal from 'Artist/MoveArtist/MoveArtistModal';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Popover from 'Components/Tooltip/Popover';
import { icons, inputTypes, kinds, sizes, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './EditArtistModalContent.css';

class EditArtistModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isConfirmMoveModalOpen: false
    };
  }

  //
  // Listeners

  onCancelPress = () => {
    this.setState({ isConfirmMoveModalOpen: false });
  };

  onSavePress = () => {
    const {
      isPathChanging,
      onSavePress
    } = this.props;

    if (isPathChanging && !this.state.isConfirmMoveModalOpen) {
      this.setState({ isConfirmMoveModalOpen: true });
    } else {
      this.setState({ isConfirmMoveModalOpen: false });

      onSavePress(false);
    }
  };

  onMoveArtistPress = () => {
    this.setState({ isConfirmMoveModalOpen: false });

    this.props.onSavePress(true);
  };

  //
  // Render

  render() {
    const {
      artistName,
      item,
      isSaving,
      showMetadataProfile,
      originalPath,
      onInputChange,
      onModalClose,
      onDeleteArtistPress,
      ...otherProps
    } = this.props;

    const {
      monitored,
      monitorNewItems,
      qualityProfileId,
      metadataProfileId,
      path,
      tags
    } = item;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Edit - {artistName}
        </ModalHeader>

        <ModalBody>
          <Form {...otherProps}>
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
                {translate('MonitorNewItems')}

                <Popover
                  anchor={
                    <Icon
                      className={styles.labelIcon}
                      name={icons.INFO}
                    />
                  }
                  title={translate('MonitorNewItems')}
                  body={<ArtistMonitorNewItemsOptionsPopoverContent />}
                  position={tooltipPositions.RIGHT}
                />
              </FormLabel>

              <FormInputGroup
                type={inputTypes.MONITOR_NEW_ITEMS_SELECT}
                name="monitorNewItems"
                helpText={translate('MonitorNewItemsHelpText')}
                {...monitorNewItems}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup size={sizes.MEDIUM}>
              <FormLabel>
                {translate('QualityProfile')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.QUALITY_PROFILE_SELECT}
                name="qualityProfileId"
                {...qualityProfileId}
                onChange={onInputChange}
              />
            </FormGroup>

            {
              showMetadataProfile ?
                <FormGroup size={sizes.MEDIUM}>
                  <FormLabel>
                    {translate('MetadataProfile')}

                    <Popover
                      anchor={
                        <Icon
                          className={styles.labelIcon}
                          name={icons.INFO}
                        />
                      }
                      title={translate('MetadataProfile')}
                      body={<ArtistMetadataProfilePopoverContent />}
                      position={tooltipPositions.RIGHT}
                    />

                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.METADATA_PROFILE_SELECT}
                    name="metadataProfileId"
                    helpText={translate('MetadataProfileIdHelpText')}
                    includeNone={true}
                    {...metadataProfileId}
                    onChange={onInputChange}
                  />
                </FormGroup> :
                null
            }

            <FormGroup size={sizes.MEDIUM}>
              <FormLabel>
                {translate('Path')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.PATH}
                name="path"
                {...path}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup size={sizes.MEDIUM}>
              <FormLabel>
                {translate('Tags')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.TAG}
                name="tags"
                {...tags}
                onChange={onInputChange}
              />
            </FormGroup>
          </Form>
        </ModalBody>
        <ModalFooter>
          <Button
            className={styles.deleteButton}
            kind={kinds.DANGER}
            onPress={onDeleteArtistPress}
          >
            {translate('Delete')}
          </Button>

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

        <MoveArtistModal
          originalPath={originalPath}
          destinationPath={path.value}
          isOpen={this.state.isConfirmMoveModalOpen}
          onModalClose={this.onCancelPress}
          onSavePress={this.onSavePress}
          onMoveArtistPress={this.onMoveArtistPress}
        />

      </ModalContent>
    );
  }
}

EditArtistModalContent.propTypes = {
  artistId: PropTypes.number.isRequired,
  artistName: PropTypes.string.isRequired,
  item: PropTypes.object.isRequired,
  isSaving: PropTypes.bool.isRequired,
  showMetadataProfile: PropTypes.bool.isRequired,
  isPathChanging: PropTypes.bool.isRequired,
  originalPath: PropTypes.string.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onDeleteArtistPress: PropTypes.func.isRequired
};

export default EditArtistModalContent;
