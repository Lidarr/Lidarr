import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { inputTypes, sizes } from 'Helpers/Props';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import FieldSet from 'Components/FieldSet';
import PageContent from 'Components/Page/PageContent';
import PageContentBodyConnector from 'Components/Page/PageContentBodyConnector';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormLabel from 'Components/Form/FormLabel';
import FormInputGroup from 'Components/Form/FormInputGroup';
import NamingConnector from './Naming/NamingConnector';

class MediaManagement extends Component {

  //
  // Render

  render() {
    const {
      advancedSettings,
      isFetching,
      error,
      settings,
      hasSettings,
      isMono,
      onInputChange,
      onSavePress,
      ...otherProps
    } = this.props;

    const fileDateOptions = [
      { key: 'none', value: 'None' },
      { key: 'albumReleaseDate', value: 'Album Release Date' }
    ];

    return (
      <PageContent title="Media Management Settings">
        <SettingsToolbarConnector
          advancedSettings={advancedSettings}
          {...otherProps}
          onSavePress={onSavePress}
        />

        <PageContentBodyConnector>
          {
            isFetching &&
              <LoadingIndicator />
          }

          {
            !isFetching && error &&
              <div>Unable to load Media Management settings</div>
          }

          {
            hasSettings && !isFetching && !error &&
              <Form
                id="mediaManagementSettings"
                {...otherProps}
              >
                <NamingConnector />

                {
                  advancedSettings &&
                    <FieldSet legend="Folders">
                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                        size={sizes.MEDIUM}
                      >
                        <FormLabel>Create empty artist folders</FormLabel>

                        <FormInputGroup
                          type={inputTypes.CHECK}
                          name="createEmptyArtistFolders"
                          helpText="Create missing artist folders during disk scan"
                          onChange={onInputChange}
                          {...settings.createEmptyArtistFolders}
                        />
                      </FormGroup>

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                        size={sizes.MEDIUM}
                      >
                        <FormLabel>Delete empty folders</FormLabel>

                        <FormInputGroup
                          type={inputTypes.CHECK}
                          name="deleteEmptyFolders"
                          helpText="Delete empty artist and album folders during disk scan and when track files are deleted"
                          onChange={onInputChange}
                          {...settings.deleteEmptyFolders}
                        />
                      </FormGroup>
                    </FieldSet>
                }

                {
                  advancedSettings &&
                    <FieldSet
                      legend="Importing"
                    >
                      {
                        isMono &&
                          <FormGroup
                            advancedSettings={advancedSettings}
                            isAdvanced={true}
                            size={sizes.MEDIUM}
                          >
                            <FormLabel>Skip Free Space Check</FormLabel>

                            <FormInputGroup
                              type={inputTypes.CHECK}
                              name="skipFreeSpaceCheckWhenImporting"
                              helpText="Use when Lidarr is unable to detect free space from your artist root folder"
                              onChange={onInputChange}
                              {...settings.skipFreeSpaceCheckWhenImporting}
                            />
                          </FormGroup>
                      }

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                        size={sizes.MEDIUM}
                      >
                        <FormLabel>Use Hardlinks instead of Copy</FormLabel>

                        <FormInputGroup
                          type={inputTypes.CHECK}
                          name="copyUsingHardlinks"
                          helpText="Use Hardlinks when trying to copy files from torrents that are still being seeded"
                          helpTextWarning="Occasionally, file locks may prevent renaming files that are being seeded. You may temporarily disable seeding and use Lidarr's rename function as a work around."
                          onChange={onInputChange}
                          {...settings.copyUsingHardlinks}
                        />
                      </FormGroup>

                      <FormGroup size={sizes.MEDIUM}>
                        <FormLabel>Import Extra Files</FormLabel>

                        <FormInputGroup
                          type={inputTypes.CHECK}
                          name="importExtraFiles"
                          helpText="Import matching extra files (subtitles, nfo, etc) after importing an track file"
                          onChange={onInputChange}
                          {...settings.importExtraFiles}
                        />
                      </FormGroup>

                      {
                        settings.importExtraFiles.value &&
                          <FormGroup
                            advancedSettings={advancedSettings}
                            isAdvanced={true}
                          >
                            <FormLabel>Import Extra Files</FormLabel>

                            <FormInputGroup
                              type={inputTypes.TEXT}
                              name="extraFileExtensions"
                              helpText="Comma separated list of extra files to import, ie sub,nfo (.nfo will be imported as .nfo-orig)"
                              onChange={onInputChange}
                              {...settings.extraFileExtensions}
                            />
                          </FormGroup>
                      }
                    </FieldSet>
                }

                <FieldSet
                  legend="File Management"
                >
                  <FormGroup size={sizes.MEDIUM}>
                    <FormLabel>Ignore Deleted Tracks</FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="autoUnmonitorPreviouslyDownloadedTracks"
                      helpText="Tracks deleted from disk are automatically unmonitored in Lidarr"
                      onChange={onInputChange}
                      {...settings.autoUnmonitorPreviouslyDownloadedTracks}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                    size={sizes.MEDIUM}
                  >
                    <FormLabel>Download Propers</FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="autoDownloadPropers"
                      helpText="Should Lidarr automatically upgrade to propers when available?"
                      onChange={onInputChange}
                      {...settings.autoDownloadPropers}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                    size={sizes.MEDIUM}
                  >
                    <FormLabel>Analyse audio files</FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="enableMediaInfo"
                      helpText="Extract audio information such as bitrate, runtime and codec information from files. This requires Lidarr to read parts of the file which may cause high disk or network activity during scans."
                      onChange={onInputChange}
                      {...settings.enableMediaInfo}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                  >
                    <FormLabel>Change File Date</FormLabel>

                    <FormInputGroup
                      type={inputTypes.SELECT}
                      name="fileDate"
                      helpText="Change file date on import/rescan"
                      values={fileDateOptions}
                      onChange={onInputChange}
                      {...settings.fileDate}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                  >
                    <FormLabel>Recycling Bin</FormLabel>

                    <FormInputGroup
                      type={inputTypes.PATH}
                      name="recycleBin"
                      helpText="Track files will go here when deleted instead of being permanently deleted"
                      onChange={onInputChange}
                      {...settings.recycleBin}
                    />
                  </FormGroup>
                </FieldSet>

                {
                  advancedSettings && isMono &&
                    <FieldSet
                      legend="Permissions"
                    >
                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                        size={sizes.MEDIUM}
                      >
                        <FormLabel>Set Permissions</FormLabel>

                        <FormInputGroup
                          type={inputTypes.CHECK}
                          name="setPermissionsLinux"
                          helpText="Should chmod/chown be run when files are imported/renamed?"
                          helpTextWarning="If you're unsure what these settings do, do not alter them."
                          onChange={onInputChange}
                          {...settings.setPermissionsLinux}
                        />
                      </FormGroup>

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                      >
                        <FormLabel>File chmod mode</FormLabel>

                        <FormInputGroup
                          type={inputTypes.TEXT}
                          name="fileChmod"
                          helpText="Octal, applied to media files when imported/renamed by Lidarr"
                          onChange={onInputChange}
                          {...settings.fileChmod}
                        />
                      </FormGroup>

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                      >
                        <FormLabel>Folder chmod mode</FormLabel>

                        <FormInputGroup
                          type={inputTypes.TEXT}
                          name="folderChmod"
                          helpText="Octal, applied to artist/album folders created by Lidarr"
                          values={fileDateOptions}
                          onChange={onInputChange}
                          {...settings.folderChmod}
                        />
                      </FormGroup>

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                      >
                        <FormLabel>chown User</FormLabel>

                        <FormInputGroup
                          type={inputTypes.TEXT}
                          name="chownUser"
                          helpText="Username or uid. Use uid for remote file systems."
                          values={fileDateOptions}
                          onChange={onInputChange}
                          {...settings.chownUser}
                        />
                      </FormGroup>

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                      >
                        <FormLabel>chown Group</FormLabel>

                        <FormInputGroup
                          type={inputTypes.TEXT}
                          name="chownGroup"
                          helpText="Group name or gid. Use gid for remote file systems."
                          values={fileDateOptions}
                          onChange={onInputChange}
                          {...settings.chownGroup}
                        />
                      </FormGroup>
                    </FieldSet>
                }
              </Form>
          }
        </PageContentBodyConnector>
      </PageContent>
    );
  }

}

MediaManagement.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  isMono: PropTypes.bool.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default MediaManagement;
