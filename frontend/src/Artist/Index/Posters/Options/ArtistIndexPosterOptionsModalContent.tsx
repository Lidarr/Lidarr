import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import selectPosterOptions from 'Artist/Index/Posters/selectPosterOptions';
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
import { setArtistPosterOption } from 'Store/Actions/artistIndexActions';
import translate from 'Utilities/String/translate';

const posterSizeOptions = [
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

interface ArtistIndexPosterOptionsModalContentProps {
  onModalClose(...args: unknown[]): unknown;
}

function ArtistIndexPosterOptionsModalContent(
  props: ArtistIndexPosterOptionsModalContentProps
) {
  const { onModalClose } = props;

  const posterOptions = useSelector(selectPosterOptions);

  const {
    detailedProgressBar,
    size,
    showTitle,
    showMonitored,
    showQualityProfile,
    showNextAlbum,
    showSearchAction,
  } = posterOptions;

  const dispatch = useDispatch();

  const onPosterOptionChange = useCallback(
    ({ name, value }: { name: string; value: unknown }) => {
      dispatch(setArtistPosterOption({ [name]: value }));
    },
    [dispatch]
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('PosterOptions')}</ModalHeader>

      <ModalBody>
        <Form>
          <FormGroup>
            <FormLabel>{translate('PosterSize')}</FormLabel>

            <FormInputGroup
              type={inputTypes.SELECT}
              name="size"
              value={size}
              values={posterSizeOptions}
              onChange={onPosterOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('DetailedProgressBar')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="detailedProgressBar"
              value={detailedProgressBar}
              helpText={translate('DetailedProgressBarHelpText')}
              onChange={onPosterOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowName')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showTitle"
              value={showTitle}
              helpText={translate('ShowTitleHelpText')}
              onChange={onPosterOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowMonitored')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showMonitored"
              value={showMonitored}
              helpText={translate('ShowMonitoredHelpText')}
              onChange={onPosterOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowQualityProfile')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showQualityProfile"
              value={showQualityProfile}
              helpText={translate('ShowQualityProfileHelpText')}
              onChange={onPosterOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowNextAlbum')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showNextAlbum"
              value={showNextAlbum}
              helpText={translate('ShowNextAlbumHelpText')}
              onChange={onPosterOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowSearch')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showSearchAction"
              value={showSearchAction}
              helpText={translate('ShowSearchActionHelpText')}
              onChange={onPosterOptionChange}
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

export default ArtistIndexPosterOptionsModalContent;
