import PropTypes from 'prop-types';
import React from 'react';
import ArtistMonitorNewItemsOptionsPopoverContent from 'AddArtist/ArtistMonitorNewItemsOptionsPopoverContent';
import Alert from 'Components/Alert';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import ProviderFieldFormGroup from 'Components/Form/ProviderFieldFormGroup';
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
import AdvancedSettingsButton from 'Settings/AdvancedSettingsButton';
import formatShortTimeSpan from 'Utilities/Date/formatShortTimeSpan';
import translate from 'Utilities/String/translate';
import styles from './EditImportListModalContent.css';

function ImportListMonitoringOptionsPopoverContent() {
  return (
    <DescriptionList>
      <DescriptionListItem
        title={translate('None')}
        data={translate('NoneMonitoringOptionHelpText')}
      />

      <DescriptionListItem
        title={translate('SpecificAlbum')}
        data={translate('SpecificMonitoringOptionHelpText')}
      />

      <DescriptionListItem
        title={translate('AllArtistAlbums')}
        data={translate('AllMonitoringOptionHelpText')}
      />
    </DescriptionList>
  );
}

function EditImportListModalContent(props) {

  const monitorOptions = [
    { key: 'none', value: translate('None') },
    { key: 'specificAlbum', value: translate('SpecificAlbum') },
    { key: 'entireArtist', value: translate('AllArtistAlbums') }
  ];

  const {
    advancedSettings,
    isFetching,
    error,
    isSaving,
    isTesting,
    saveError,
    item,
    onInputChange,
    onFieldChange,
    onModalClose,
    onSavePress,
    onTestPress,
    onAdvancedSettingsPress,
    onDeleteImportListPress,
    showMetadataProfile,
    ...otherProps
  } = props;

  const {
    id,
    implementationName,
    name,
    enableAutomaticAdd,
    minRefreshInterval,
    shouldMonitor,
    shouldMonitorExisting,
    shouldSearch,
    rootFolderPath,
    monitorNewItems,
    qualityProfileId,
    metadataProfileId,
    tags,
    fields,
    message
  } = item;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id ? translate('EditImportListImplementation', { implementationName }) : translate('AddImportListImplementation', { implementationName })}
      </ModalHeader>

      <ModalBody>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && !!error &&
            <div>
              {translate('UnableToAddANewListPleaseTryAgain')}
            </div>
        }

        {
          !isFetching && !error &&
            <Form {...otherProps}>
              {
                !!message &&
                  <Alert
                    className={styles.message}
                    kind={message.value.type}
                  >
                    {message.value.message}
                  </Alert>
              }

              <Alert
                kind={kinds.INFO}
                className={styles.message}
              >
                {translate('ListWillRefreshEveryInterp', [formatShortTimeSpan(minRefreshInterval.value)])}
              </Alert>

              <FieldSet legend={translate('ImportListSettings')} >
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
                    {translate('EnableAutomaticAdd')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.CHECK}
                    name="enableAutomaticAdd"
                    helpText={translate('EnableAutomaticAddHelpText')}
                    {...enableAutomaticAdd}
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
                      body={<ImportListMonitoringOptionsPopoverContent />}
                      position={tooltipPositions.RIGHT}
                    />
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.SELECT}
                    name="shouldMonitor"
                    values={monitorOptions}
                    helpText={translate('ShouldMonitorHelpText')}
                    {...shouldMonitor}
                    onChange={onInputChange}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>
                    {translate('ShouldMonitorExisting')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.CHECK}
                    name="shouldMonitorExisting"
                    helpText={translate('ShouldMonitorExistingHelpText')}
                    {...shouldMonitorExisting}
                    onChange={onInputChange}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>
                    {translate('ShouldSearch')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.CHECK}
                    name="shouldSearch"
                    helpText={translate('ShouldSearchHelpText')}
                    {...shouldSearch}
                    onChange={onInputChange}
                  />
                </FormGroup>
              </FieldSet>

              <FieldSet legend={translate('AddedArtistSettings')} >
                <FormGroup>
                  <FormLabel>
                    {translate('RootFolder')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.ROOT_FOLDER_SELECT}
                    name="rootFolderPath"
                    helpText={translate('RootFolderPathHelpText')}
                    {...rootFolderPath}
                    includeMissingValue={true}
                    onChange={onInputChange}
                  />
                </FormGroup>

                <FormGroup>
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

                <FormGroup>
                  <FormLabel>
                    {translate('QualityProfile')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.QUALITY_PROFILE_SELECT}
                    name="qualityProfileId"
                    helpText={translate('QualityProfileIdHelpText')}
                    {...qualityProfileId}
                    onChange={onInputChange}
                  />
                </FormGroup>

                <FormGroup className={showMetadataProfile ? undefined : styles.hideMetadataProfile}>
                  <FormLabel>
                    {translate('MetadataProfile')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.METADATA_PROFILE_SELECT}
                    name="metadataProfileId"
                    helpText={translate('MetadataProfileIdHelpText')}
                    {...metadataProfileId}
                    includeNone={true}
                    onChange={onInputChange}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>
                    {translate('LidarrTags')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.TAG}
                    name="tags"
                    helpText={translate('TagsHelpText')}
                    {...tags}
                    onChange={onInputChange}
                  />
                </FormGroup>
              </FieldSet>

              {
                !!fields && !!fields.length &&
                  <FieldSet legend={translate('ImportListSpecificSettings')} >
                    {
                      fields.map((field) => {
                        return (
                          <ProviderFieldFormGroup
                            key={field.name}
                            advancedSettings={advancedSettings}
                            provider="importList"
                            providerData={item}
                            section="settings.importLists"
                            {...field}
                            onChange={onFieldChange}
                          />
                        );
                      })
                    }
                  </FieldSet>
              }

            </Form>
        }
      </ModalBody>
      <ModalFooter>
        {
          id &&
            <Button
              className={styles.deleteButton}
              kind={kinds.DANGER}
              onPress={onDeleteImportListPress}
            >
              {translate('Delete')}
            </Button>
        }

        <AdvancedSettingsButton
          advancedSettings={advancedSettings}
          onAdvancedSettingsPress={onAdvancedSettingsPress}
          showLabel={false}
        />

        <SpinnerErrorButton
          isSpinning={isTesting}
          error={saveError}
          onPress={onTestPress}
        >
          {translate('Test')}
        </SpinnerErrorButton>

        <Button
          onPress={onModalClose}
        >
          {translate('Cancel')}
        </Button>

        <SpinnerErrorButton
          isSpinning={isSaving}
          error={saveError}
          onPress={onSavePress}
        >
          {translate('Save')}
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

EditImportListModalContent.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isSaving: PropTypes.bool.isRequired,
  isTesting: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  showMetadataProfile: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onFieldChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onTestPress: PropTypes.func.isRequired,
  onAdvancedSettingsPress: PropTypes.func.isRequired,
  onDeleteImportListPress: PropTypes.func
};

export default EditImportListModalContent;
