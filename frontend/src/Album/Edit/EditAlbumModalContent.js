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

function matchesFilter(release, trackCounts, mediumCounts) {
  if (trackCounts.length && !trackCounts.includes(release.trackCount)) {
    return false;
  }

  if (mediumCounts.length && !mediumCounts.includes(release.mediumCount)) {
    return false;
  }

  return true;
}

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
      advancedSettings,
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
      preferredTrackCounts,
      preferredMediumCounts,
      autoBlocklistUnwantedRelease,
      releases
    } = item;

    const trackCounts = preferredTrackCounts.value || [];
    const mediumCounts = preferredMediumCounts.value || [];
    const allReleases = releases.value || [];
    const hasFilter = trackCounts.length > 0 || mediumCounts.length > 0;
    const matchingReleases = allReleases.filter((release) => matchesFilter(release, trackCounts, mediumCounts));

    let filterSummary = null;
    let filterSummaryIsError = false;

    if (hasFilter) {
      if (matchingReleases.length === 0) {
        filterSummary = translate('ReleaseFilterNoMatches');
        filterSummaryIsError = true;
      } else {
        filterSummary = translate('ReleaseFilterMatches', {
          matched: matchingReleases.length,
          total: allReleases.length
        });
      }
    }

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
                {translate('PreferredTrackCount')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.ALBUM_RELEASE_COUNT_SELECT}
                name="preferredTrackCounts"
                countKey="trackCount"
                helpText={translate('PreferredTrackCountHelpText')}
                albumReleases={releases}
                {...preferredTrackCounts}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup size={sizes.MEDIUM}>
              <FormLabel>
                {translate('PreferredMediumCount')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.ALBUM_RELEASE_COUNT_SELECT}
                name="preferredMediumCounts"
                countKey="mediumCount"
                helpText={translate('PreferredMediumCountHelpText')}
                helpTextWarning={filterSummaryIsError ? filterSummary : undefined}
                helpTexts={filterSummary && !filterSummaryIsError ? [filterSummary] : undefined}
                albumReleases={releases}
                {...preferredMediumCounts}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup size={sizes.MEDIUM}>
              <FormLabel>
                {translate('AutoBlocklistUnwantedRelease')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="autoBlocklistUnwantedRelease"
                helpText={translate('AutoBlocklistUnwantedReleaseHelpText')}
                isDisabled={!hasFilter}
                {...autoBlocklistUnwantedRelease}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup
              size={sizes.MEDIUM}
              advancedSettings={advancedSettings}
              isAdvanced={true}
            >
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
            isDisabled={filterSummaryIsError}
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
  advancedSettings: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

EditAlbumModalContent.defaultProps = {
  advancedSettings: false
};

export default EditAlbumModalContent;
