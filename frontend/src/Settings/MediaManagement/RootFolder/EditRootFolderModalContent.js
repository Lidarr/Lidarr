import PropTypes from 'prop-types';
import React from 'react';
import ArtistMetadataProfilePopoverContent from 'AddArtist/ArtistMetadataProfilePopoverContent';
import ArtistMonitoringOptionsPopoverContent from 'AddArtist/ArtistMonitoringOptionsPopoverContent';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Popover from 'Components/Tooltip/Popover';
import { icons, inputTypes, kinds, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './EditRootFolderModalContent.css';

function EditRootFolderModalContent(props) {

  const {
    advancedSettings,
    isFetching,
    error,
    isSaving,
    saveError,
    item,
    onInputChange,
    onModalClose,
    onSavePress,
    onDeleteRootFolderPress,
    showMetadataProfile,
    ...otherProps
  } = props;

  const {
    id,
    name,
    path,
    defaultQualityProfileId,
    defaultMetadataProfileId,
    defaultMonitorOption,
    defaultTags
  } = item;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id ? 'Edit Root Folder' : 'Add Root Folder'}
      </ModalHeader>

      <ModalBody>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && !!error &&
            <div>
              {translate('UnableToAddANewRootFolderPleaseTryAgain')}
            </div>
        }

        {
          !isFetching && !error &&
            <Form {...otherProps}>
              <FormGroup>
                <FormLabel>
                  {translate('Name')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="name"
                  {...name}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('Path')}
                </FormLabel>

                <FormInputGroup
                  type={id ? inputTypes.TEXT : inputTypes.PATH}
                  readOnly={!!id}
                  name="path"
                  helpText={translate('PathHelpText')}
                  helpTextWarning={translate('PathHelpTextWarning')}
                  {...path}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Monitor

                  <Popover
                    anchor={
                      <Icon
                        className={styles.labelIcon}
                        name={icons.INFO}
                      />
                    }
                    title={translate('MonitoringOptions')}
                    body={<ArtistMonitoringOptionsPopoverContent />}
                    position={tooltipPositions.RIGHT}
                  />
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.MONITOR_ALBUMS_SELECT}
                  name="defaultMonitorOption"
                  onChange={onInputChange}
                  {...defaultMonitorOption}
                  helpText={translate('DefaultMonitorOptionHelpText')}
                />

              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('QualityProfile')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.QUALITY_PROFILE_SELECT}
                  name="defaultQualityProfileId"
                  helpText={translate('DefaultQualityProfileIdHelpText')}
                  {...defaultQualityProfileId}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup className={showMetadataProfile ? undefined : styles.hideMetadataProfile}>
                <FormLabel>
                  Metadata Profile
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
                  name="defaultMetadataProfileId"
                  helpText={translate('DefaultMetadataProfileIdHelpText')}
                  {...defaultMetadataProfileId}
                  includeNone={true}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('DefaultLidarrTags')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TAG}
                  name="defaultTags"
                  helpText={translate('DefaultTagsHelpText')}
                  {...defaultTags}
                  onChange={onInputChange}
                />
              </FormGroup>

            </Form>
        }
      </ModalBody>
      <ModalFooter>
        {
          id &&
            <Button
              className={styles.deleteButton}
              kind={kinds.DANGER}
              onPress={onDeleteRootFolderPress}
            >
              Delete
            </Button>
        }

        <Button
          onPress={onModalClose}
        >
          Cancel
        </Button>

        <SpinnerErrorButton
          isSpinning={isSaving}
          error={saveError}
          onPress={onSavePress}
        >
          Save
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

EditRootFolderModalContent.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  showMetadataProfile: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onDeleteRootFolderPress: PropTypes.func
};

export default EditRootFolderModalContent;
