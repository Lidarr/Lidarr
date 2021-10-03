import PropTypes from 'prop-types';
import React from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './EditReleaseProfileModalContent.css';

// Tab, enter, and comma
const tagInputDelimiters = [9, 13, 188];

function EditReleaseProfileModalContent(props) {
  const {
    isSaving,
    saveError,
    item,
    onInputChange,
    onModalClose,
    onSavePress,
    onDeleteReleaseProfilePress,
    ...otherProps
  } = props;

  const {
    id,
    enabled,
    required,
    ignored,
    preferred,
    includePreferredWhenRenaming,
    tags,
    indexerId
  } = item;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id ? 'Edit Release Profile' : 'Add Release Profile'}
      </ModalHeader>

      <ModalBody>
        <Form {...otherProps}>
          <FormGroup>
            <FormLabel>
              {translate('EnableProfile')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="enabled"
              helpText={translate('EnabledHelpText')}
              {...enabled}
              onChange={onInputChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>
              {translate('MustContain')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.TEXT_TAG}
              name="required"
              helpText={translate('RequiredHelpText')}
              kind={kinds.SUCCESS}
              placeholder={translate('RequiredPlaceHolder')}
              delimiters={tagInputDelimiters}
              {...required}
              onChange={onInputChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>
              {translate('MustNotContain')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.TEXT_TAG}
              name="ignored"
              helpText={translate('IgnoredHelpText')}
              kind={kinds.DANGER}
              placeholder={translate('IgnoredPlaceHolder')}
              delimiters={tagInputDelimiters}
              {...ignored}
              onChange={onInputChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>
              {translate('Preferred')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.KEY_VALUE_LIST}
              name="preferred"
              helpTexts={[
                translate('PreferredHelpTexts1'),
                translate('PreferredHelpTexts2'),
                translate('PreferredHelpTexts3')
              ]}
              {...preferred}
              keyPlaceholder={translate('Term')}
              valuePlaceholder={translate('Score')}
              onChange={onInputChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>
              {translate('IncludePreferredWhenRenaming')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="includePreferredWhenRenaming"
              helpText={indexerId.value === 0 ? translate('IndexerIdvalue0IncludeInPreferredWordsRenamingFormat') : translate('IndexerIdvalue0OnlySupportedWhenIndexerIsSetToAll')}
              {...includePreferredWhenRenaming}
              onChange={onInputChange}
              isDisabled={indexerId.value !== 0}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>
              {translate('Indexer')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.INDEXER_SELECT}
              name="indexerId"
              helpText={translate('IndexerIdHelpText')}
              helpTextWarning={translate('IndexerIdHelpTextWarning')}
              {...indexerId}
              includeAny={true}
              onChange={onInputChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>
              {translate('Tags')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.TAG}
              name="tags"
              helpText={translate('TagsHelpText')}
              {...tags}
              onChange={onInputChange}
            />
          </FormGroup>
        </Form>
      </ModalBody>
      <ModalFooter>
        {
          id &&
            <Button
              className={styles.deleteButton}
              kind={kinds.DANGER}
              onPress={onDeleteReleaseProfilePress}
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

EditReleaseProfileModalContent.propTypes = {
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onDeleteReleaseProfilePress: PropTypes.func
};

export default EditReleaseProfileModalContent;
