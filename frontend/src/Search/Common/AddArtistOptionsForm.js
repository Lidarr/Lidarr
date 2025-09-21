import PropTypes from 'prop-types';
import React, { Component } from 'react';
import ArtistMetadataProfilePopoverContent from 'AddArtist/ArtistMetadataProfilePopoverContent';
import ArtistMonitoringOptionsPopoverContent from 'AddArtist/ArtistMonitoringOptionsPopoverContent';
import ArtistMonitorNewItemsOptionsPopoverContent from 'AddArtist/ArtistMonitorNewItemsOptionsPopoverContent';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Popover from 'Components/Tooltip/Popover';
import { icons, inputTypes, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import SearchAliasInput from 'Artist/Edit/SearchAliasInput';
import styles from './AddArtistOptionsForm.css';

class AddArtistOptionsForm extends Component {

  //
  // Listeners

  onQualityProfileIdChange = ({ value }) => {
    this.props.onInputChange({ name: 'qualityProfileId', value: parseInt(value) });
  };

  onMetadataProfileIdChange = ({ value }) => {
    this.props.onInputChange({ name: 'metadataProfileId', value: parseInt(value) });
  };

  //
  // Render

  render() {
    const {
      rootFolderPath,
      monitor,
      monitorNewItems,
      qualityProfileId,
      metadataProfileId,
      includeNoneMetadataProfile,
      showMetadataProfile,
      folder,
      tags,
      isWindows,
      aliases,
      artistName,
      searchAlias,
      onInputChange,
      ...otherProps
    } = this.props;

    return (
      <Form {...otherProps}>
        <FormGroup>
          <FormLabel>
            {translate('RootFolder')}
          </FormLabel>

          <FormInputGroup
            type={inputTypes.ROOT_FOLDER_SELECT}
            name="rootFolderPath"
            valueOptions={{
              artistFolder: folder,
              isWindows
            }}
            selectedValueOptions={{
              artistFolder: folder,
              isWindows
            }}
            helpText={translate('AddNewArtistRootFolderHelpText', { folder })}
            onChange={onInputChange}
            {...rootFolderPath}
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
            name="monitor"
            helpText={translate('MonitoringOptionsHelpText')}
            onChange={onInputChange}
            {...monitor}
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
            {translate('SearchAlias')}
          </FormLabel>

          <SearchAliasInput
            aliases={aliases}
            artistName={artistName}
            {...searchAlias}
            onInputChange={onInputChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>
            {translate('QualityProfile')}
          </FormLabel>

          <FormInputGroup
            type={inputTypes.QUALITY_PROFILE_SELECT}
            name="qualityProfileId"
            onChange={this.onQualityProfileIdChange}
            {...qualityProfileId}
          />
        </FormGroup>

        <FormGroup className={showMetadataProfile ? undefined : styles.hideMetadataProfile}>
          <FormLabel>
            Metadata Profile

            {
              includeNoneMetadataProfile &&
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
            }
          </FormLabel>

          <FormInputGroup
            type={inputTypes.METADATA_PROFILE_SELECT}
            name="metadataProfileId"
            includeNone={includeNoneMetadataProfile}
            onChange={this.onMetadataProfileIdChange}
            {...metadataProfileId}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>
            {translate('Tags')}
          </FormLabel>

          <FormInputGroup
            type={inputTypes.TAG}
            name="tags"
            onChange={onInputChange}
            {...tags}
          />
        </FormGroup>
      </Form>
    );
  }
}

AddArtistOptionsForm.propTypes = {
  rootFolderPath: PropTypes.object,
  monitor: PropTypes.object.isRequired,
  monitorNewItems: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.object,
  metadataProfileId: PropTypes.object,
  showMetadataProfile: PropTypes.bool.isRequired,
  includeNoneMetadataProfile: PropTypes.bool.isRequired,
  folder: PropTypes.string.isRequired,
  tags: PropTypes.object.isRequired,
  aliases: PropTypes.arrayOf(PropTypes.string),
  artistName: PropTypes.string.isRequired,
  searchAlias: PropTypes.object.isRequired,
  isWindows: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default AddArtistOptionsForm;
