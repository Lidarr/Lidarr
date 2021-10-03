import PropTypes from 'prop-types';
import React from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormInputHelpText from 'Components/Form/FormInputHelpText';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './NotificationEventItems.css';

function NotificationEventItems(props) {
  const {
    item,
    onInputChange
  } = props;

  const {
    onGrab,
    onReleaseImport,
    onUpgrade,
    onRename,
    onHealthIssue,
    onDownloadFailure,
    onImportFailure,
    onTrackRetag,
    supportsOnGrab,
    supportsOnReleaseImport,
    supportsOnUpgrade,
    supportsOnRename,
    supportsOnHealthIssue,
    includeHealthWarnings,
    supportsOnDownloadFailure,
    supportsOnImportFailure,
    supportsOnTrackRetag
  } = item;

  return (
    <FormGroup>
      <FormLabel>
        {translate('NotificationTriggers')}
      </FormLabel>
      <div>
        <FormInputHelpText
          text="Select which events should trigger this notification"
          link="https://wiki.servarr.com/lidarr/settings#connections"
        />
        <div className={styles.events}>
          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onGrab"
              helpText={translate('OnGrabHelpText')}
              isDisabled={!supportsOnGrab.value}
              {...onGrab}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onReleaseImport"
              helpText={translate('OnReleaseImportHelpText')}
              isDisabled={!supportsOnReleaseImport.value}
              {...onReleaseImport}
              onChange={onInputChange}
            />
          </div>

          {
            onReleaseImport.value &&
              <div>
                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="onUpgrade"
                  helpText={translate('OnUpgradeHelpText')}
                  isDisabled={!supportsOnUpgrade.value}
                  {...onUpgrade}
                  onChange={onInputChange}
                />
              </div>
          }

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onDownloadFailure"
              helpText={translate('OnDownloadFailureHelpText')}
              isDisabled={!supportsOnDownloadFailure.value}
              {...onDownloadFailure}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onImportFailure"
              helpText={translate('OnImportFailureHelpText')}
              isDisabled={!supportsOnImportFailure.value}
              {...onImportFailure}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onRename"
              helpText={translate('OnRenameHelpText')}
              isDisabled={!supportsOnRename.value}
              {...onRename}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onTrackRetag"
              helpText={translate('OnTrackRetagHelpText')}
              isDisabled={!supportsOnTrackRetag.value}
              {...onTrackRetag}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onHealthIssue"
              helpText={translate('OnHealthIssueHelpText')}
              isDisabled={!supportsOnHealthIssue.value}
              {...onHealthIssue}
              onChange={onInputChange}
            />
          </div>

          {
            onHealthIssue.value &&
              <div>
                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="includeHealthWarnings"
                  helpText={translate('IncludeHealthWarningsHelpText')}
                  isDisabled={!supportsOnHealthIssue.value}
                  {...includeHealthWarnings}
                  onChange={onInputChange}
                />
              </div>
          }
        </div>
      </div>
    </FormGroup>
  );
}

NotificationEventItems.propTypes = {
  item: PropTypes.object.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default NotificationEventItems;
