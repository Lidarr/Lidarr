import PropTypes from 'prop-types';
import React from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import PrimaryTypeItems from './PrimaryTypeItems';
import ReleaseStatusItems from './ReleaseStatusItems';
import SecondaryTypeItems from './SecondaryTypeItems';
import styles from './EditMetadataProfileModalContent.css';

function EditMetadataProfileModalContent(props) {
  const {
    isFetching,
    error,
    isSaving,
    saveError,
    primaryAlbumTypes,
    secondaryAlbumTypes,
    item,
    isInUse,
    onInputChange,
    onSavePress,
    onModalClose,
    onDeleteMetadataProfilePress,
    ...otherProps
  } = props;

  const {
    id,
    name,
    primaryAlbumTypes: itemPrimaryAlbumTypes,
    secondaryAlbumTypes: itemSecondaryAlbumTypes,
    releaseStatuses: itemReleaseStatuses
  } = item;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id ? 'Edit Metadata Profile' : 'Add Metadata Profile'}
      </ModalHeader>

      <ModalBody>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && !!error &&
            <div>
              {translate('UnableToAddANewMetadataProfilePleaseTryAgain')}
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

              <PrimaryTypeItems
                metadataProfileItems={itemPrimaryAlbumTypes.value}
                errors={itemPrimaryAlbumTypes.errors}
                warnings={itemPrimaryAlbumTypes.warnings}
                formLabel={translate('PrimaryAlbumTypes')}
                {...otherProps}
              />

              <SecondaryTypeItems
                metadataProfileItems={itemSecondaryAlbumTypes.value}
                errors={itemSecondaryAlbumTypes.errors}
                warnings={itemSecondaryAlbumTypes.warnings}
                formLabel={translate('SecondaryAlbumTypes')}
                {...otherProps}
              />

              <ReleaseStatusItems
                metadataProfileItems={itemReleaseStatuses.value}
                errors={itemReleaseStatuses.errors}
                warnings={itemReleaseStatuses.warnings}
                formLabel={translate('ReleaseStatuses')}
                {...otherProps}
              />

            </Form>
        }
      </ModalBody>
      <ModalFooter>
        {
          id &&
            <div
              className={styles.deleteButtonContainer}
              title={isInUse ? translate('IsInUseCantDeleteAMetadataProfileThatIsAttachedToAnArtistOrImportList') : undefined}
            >
              <Button
                kind={kinds.DANGER}
                isDisabled={isInUse}
                onPress={onDeleteMetadataProfilePress}
              >
                Delete
              </Button>
            </div>
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

EditMetadataProfileModalContent.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  primaryAlbumTypes: PropTypes.arrayOf(PropTypes.object).isRequired,
  secondaryAlbumTypes: PropTypes.arrayOf(PropTypes.object).isRequired,
  releaseStatuses: PropTypes.arrayOf(PropTypes.object).isRequired,
  item: PropTypes.object.isRequired,
  isInUse: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onDeleteMetadataProfilePress: PropTypes.func
};

export default EditMetadataProfileModalContent;
