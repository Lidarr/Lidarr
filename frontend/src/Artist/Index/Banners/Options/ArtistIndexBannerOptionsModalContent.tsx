import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import selectBannerOptions from 'Artist/Index/Banners/selectBannerOptions';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
import { setArtistBannerOption } from 'Store/Actions/artistIndexActions';
import translate from 'Utilities/String/translate';

const bannerSizeOptions = [
  {
    key: 'small',
    get value() {
      return translate('Small');
    },
  },
  {
    key: 'medium',
    get value() {
      return translate('Medium');
    },
  },
  {
    key: 'large',
    get value() {
      return translate('Large');
    },
  },
];

interface ArtistIndexBannerOptionsModalContentProps {
  onModalClose(...args: unknown[]): unknown;
}

function ArtistIndexBannerOptionsModalContent(
  props: ArtistIndexBannerOptionsModalContentProps
) {
  const { onModalClose } = props;

  const bannerOptions = useSelector(selectBannerOptions);

  const {
    detailedProgressBar,
    size,
    showTitle,
    showMonitored,
    showQualityProfile,
    showNextAlbum,
    showSearchAction,
  } = bannerOptions;

  const dispatch = useDispatch();

  const onBannerOptionChange = useCallback(
    ({ name, value }: { name: string; value: unknown }) => {
      dispatch(setArtistBannerOption({ [name]: value }));
    },
    [dispatch]
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('BannerOptions')}</ModalHeader>

      <ModalBody>
        <Form>
          <FormGroup>
            <FormLabel>{translate('BannerSize')}</FormLabel>

            <FormInputGroup
              type={inputTypes.SELECT}
              name="size"
              value={size}
              values={bannerSizeOptions}
              onChange={onBannerOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('DetailedProgressBar')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="detailedProgressBar"
              value={detailedProgressBar}
              helpText={translate('DetailedProgressBarHelpText')}
              onChange={onBannerOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowName')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showTitle"
              value={showTitle}
              helpText={translate('ShowTitleHelpText')}
              onChange={onBannerOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowMonitored')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showMonitored"
              value={showMonitored}
              helpText={translate('ShowMonitoredHelpText')}
              onChange={onBannerOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowQualityProfile')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showQualityProfile"
              value={showQualityProfile}
              helpText={translate('ShowQualityProfileHelpText')}
              onChange={onBannerOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowNextAlbum')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showNextAlbum"
              value={showNextAlbum}
              helpText={translate('ShowNextAlbumHelpText')}
              onChange={onBannerOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowSearch')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showSearchAction"
              value={showSearchAction}
              helpText={translate('ShowSearchActionHelpText')}
              onChange={onBannerOptionChange}
            />
          </FormGroup>
        </Form>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default ArtistIndexBannerOptionsModalContent;
